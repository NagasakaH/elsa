# 起動時にローカル JSON を読み込んで実行する手順

## 概要
- `IWorkflowDefinitionImporter` を使い、起動時にローカル配置したワークフロー JSON をインポートできます。
- インポート後は API (`/workflow-definitions/{definitionId}/dispatch` など) で任意のワークフローを実行できます。
- JSON のどのフィールドが実行時パラメータと結び付くかを最後に整理しています。

## 前提
- Elsa 3 サーバープロジェクト（例: `Elsa.Server.Web`）に以下の Hosted Service を追加する想定。
- ローカルに配置した JSON は最新のスキーマ (例: `$schema: https://elsaworkflows.io/schemas/workflow-definition/v3.0.0/schema.json`) 形式であること。

## 起動時インポートのコード例
`WorkflowSeedHostedService` を追加し、`Startup/Program` で登録します。

```csharp
using System.Text.Json;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

public class WorkflowSeedHostedService : IHostedService
{
    private readonly IWorkflowDefinitionImporter _importer;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public WorkflowSeedHostedService(IWorkflowDefinitionImporter importer) => _importer = importer;

    public async Task StartAsync(CancellationToken ct)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "workflows"); // JSON 配置先を決める
        if (!Directory.Exists(folder)) return;

        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file, ct);
            var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, _jsonOptions)!;

            await _importer.ImportAsync(new SaveWorkflowDefinitionRequest
            {
                Model = model,
                // 即公開したい場合は true。既存 JSON に isPublished=true が含まれるならそれに従う。
                Publish = model.IsPublished
            }, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

`Program.cs` などで登録:
```csharp
builder.Services.AddHostedService<WorkflowSeedHostedService>();
```

JSON 配置例:
```
<content-root>/workflows/mermaid-sequence-sample.json
```
`mermaid-sequence-sample.json` は `doc/qa/samples/mermaid-sequence-sample.json` の内容をそのまま利用できます。

## インポート後にワークフローを実行する
- API クライアント無しで動かす簡易例 (`curl`):

### Dispatch (非同期キュー投入)
```bash
curl -X POST "http://localhost:13000/workflow-definitions/f4b6b8e83d8a4b1b/dispatch" \
  -H "Content-Type: application/json" \
  -d '{
    "instanceId": null,
    "correlationId": "demo-corr-1",
    "triggerActivityId": null,
    "versionOptions": null,
    "input": {
      "message": "hello from api"
    }
  }'
```

### Execute (即時実行)
```bash
curl -X POST "http://localhost:13000/workflow-definitions/f4b6b8e83d8a4b1b/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "input": {
      "message": "hello immediate"
    }
  }'
```
- `f4b6b8e83d8a4b1b` はサンプル JSON の `definitionId`。自分の JSON に合わせて置き換えてください。
- `versionOptions` を指定すると特定バージョンを実行可能（例: `{ "version": 2 }` や `{ "isPublished": true }`）。

### .NET (C#) から実行するサンプル
`Elsa.Api.Client` を使う例。`definitionId` と `input` を差し替えてください。

```csharp
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

// Program.cs (DI 登録)
builder.Services.AddElsaApiClients(options =>
{
  options.ServerUrl = "http://localhost:13000"; // Elsa サーバー URL
});

// どこかのサービス/ページで実行
public class WorkflowRunner
{
  private readonly IExecuteWorkflowApi _executeApi;

  public WorkflowRunner(IExecuteWorkflowApi executeApi)
  {
    _executeApi = executeApi;
  }

  public async Task RunAsync(CancellationToken ct = default)
  {
    var definitionId = "f4b6b8e83d8a4b1b"; // インポート済みワークフローの definitionId

    var request = new DispatchWorkflowDefinitionRequest
    {
      CorrelationId = "demo-corr-1",
      Input = new
      {
        message = "hello from code"
      }
      // TriggerActivityId = null,
      // VersionOptions = new VersionOptions { IsPublished = true }
    };

    var response = await _executeApi.DispatchAsync(definitionId, request, ct);
    response.EnsureSuccessStatusCode();
  }
}
```

メモ:
- 同期的に結果を待ちたい場合は `DispatchAsync` ではなく `ExecuteAsync` を利用します。
- `input` オブジェクトのキーはワークフロー側の `inputs` 名に合わせて渡してください。

## JSON フィールドと実行パラメータの対応
- `definitionId` (JSON) ⇔ 実行リクエストのパスパラメータ `{definitionId}`。この値が一致していれば対象が特定されます。
- `version` / `isPublished` / `isLatest` (JSON) ⇔ 実行リクエストの `versionOptions`。省略時は既定の Published/Latest が使われます。
- `id` (JSON) はそのバージョンの内部 ID。通常、実行 API では `definitionId` を使います。
- `inputs` 定義 (JSON) ⇔ `input` ペイロードの構造。`inputs` に宣言した名前に対応するキーを `input` に渡します。
- トリガー用アクティビティのプロパティ（例: `HttpEndpoint.path`） ⇔ 外部からの開始条件。サンプルでは `GET /sample` にマッピングされていますが、API 実行の場合は `dispatch/execute` で直接起動できます。
- `options.usableAsActivity` などは埋め込み用のフラグで、実行 API パラメータとは直接結びつきません。

## 運用ヒント
- 複数 JSON を配布する場合は `workflows` フォルダにまとめて配置し、起動時にまとめてインポートできます。
- 再デプロイ時に上書きしたい場合、`definitionId` を固定し、`version` を上げて `isPublished=true` で保存するか、`Publish = true` を指定して常に最新を公開する運用にできます。
- 本番ではフォルダをボリュームマウントするか CI/CD でコピーし、アプリ再起動でインポートさせるのが簡単です。
