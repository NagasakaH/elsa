---
sidebar_position: 1771571979
date: 2026-02-20T07:19:39+00:00
---

# Elsa Workflow System 基本設計書

## 1. システムアーキテクチャ概要

Elsa Workflow System は、Elsa 3.x をベースとしたワークフロー実行基盤です。Blazor WebAssembly による GUI（Elsa Studio）、ASP.NET Core による API サーバー（ElsaServer）、PostgreSQL によるデータ永続化、RabbitMQ によるメッセージングを組み合わせた分散アーキテクチャを採用しています。

```mermaid
graph TB
    subgraph "フロントエンド"
        Studio["Elsa Studio<br/>(Blazor WASM)"]
    end

    subgraph "バックエンド"
        ElsaServer["ElsaServer<br/>(ASP.NET Core 8.0)"]
        
        subgraph "Elsa Core"
            EFCore["Elsa.EntityFrameworkCore<br/>(ワークフロー永続化)"]
            Runtime["Elsa Runtime<br/>(ワークフロー実行)"]
        end
        
        subgraph "メッセージング"
            MassTransit["MassTransit<br/>(メッセージブローカー抽象化)"]
        end
    end

    subgraph "外部システム"
        PostgreSQL[(PostgreSQL<br/>ワークフロー定義・状態)]
        RabbitMQ[(RabbitMQ<br/>メッセージキュー)]
        ActivityDLLs["Activity DLLs<br/>(カスタムアクティビティ)"]
    end

    Studio <-->|HTTP/REST API| ElsaServer
    ElsaServer --> EFCore
    ElsaServer --> Runtime
    ElsaServer --> MassTransit
    EFCore <-->|Entity Framework| PostgreSQL
    MassTransit <-->|AMQP| RabbitMQ
    ElsaServer -.->|動的ロード| ActivityDLLs
```

### アーキテクチャの特徴

| 特徴 | 説明 |
|------|------|
| **プラグイン機構** | カスタムアクティビティを DLL として動的にロード可能 |
| **非同期メッセージング** | MassTransit + RabbitMQ によるワークフロー実行の非同期化 |
| **永続化** | PostgreSQL による ワークフロー定義・実行状態の永続化 |
| **GUI 管理** | Elsa Studio による ワークフローのビジュアル設計・監視 |

---

## 2. コンポーネント構成

システムは以下のプロジェクト・コンポーネントで構成されます。

| コンポーネント | プロジェクト | 責務 |
|--------------|------------|------|
| **アクティビティ契約** | `Activities.Contracts` | `IActivityModule` 等のインターフェース定義。カスタムアクティビティ DLL が実装すべき契約を提供 |
| **アクティビティローダー** | `Activities.Loader` | DLL 動的ロード・Elsa への登録を担当。`DirectoryActivityAssemblyLoader` が中核クラス |
| **サンプルアクティビティ** | `Activities.Templates.CustomActivityTemplate` | カスタムアクティビティ DLL プラグインのサンプル実装 |
| **ワークフローカタログ** | `WorkflowCatalog` | インメモリ ワークフロー定義レジストリ。`ConcurrentDictionary` で管理。`NagasakaEventSystem.WorkflowCatalog` 名前空間で独立ライブラリとして分離 |
| **ワークフロー起動** | `ElsaServer/Services/WorkflowLauncher` | ワークフロー実行のオーケストレーション。検証・実行・エラーハンドリング |
| **ステータス通知** | `ElsaServer/Services/WorkflowStatusPublisher` | ワークフロー実行状態の通知。リトライ機能付き |
| **メッセージ消費** | `ElsaServer/Consumers/StartWorkflowConsumer` | MassTransit コンシューマ。`StartWorkflowCommand` を受信してワークフロー実行 |

### コンポーネント依存関係

```mermaid
graph LR
    subgraph "Activities.Contracts"
        IActivityModule["IActivityModule"]
        ActivityModuleContext["ActivityModuleContext"]
        ActivityModuleMetadataAttribute["ActivityModuleMetadataAttribute"]
    end

    subgraph "Activities.Loader"
        DirectoryActivityAssemblyLoader["DirectoryActivityAssemblyLoader"]
        ActivityLoaderHostedService["ActivityLoaderHostedService"]
        IActivityRegistrar["IActivityRegistrar"]
    end

    subgraph "ElsaServer"
        subgraph "Services"
            WorkflowCatalog["WorkflowCatalog"]
            WorkflowCatalogLoader["WorkflowCatalogLoader"]
            WorkflowLauncher["WorkflowLauncher"]
            WorkflowStatusPublisher["WorkflowStatusPublisher"]
            RunContextStore["RunContextStore"]
        end
        
        subgraph "Consumers"
            StartWorkflowConsumer["StartWorkflowConsumer"]
        end
    end

    DirectoryActivityAssemblyLoader --> IActivityModule
    DirectoryActivityAssemblyLoader --> ActivityModuleContext
    ActivityLoaderHostedService --> DirectoryActivityAssemblyLoader
    
    StartWorkflowConsumer --> WorkflowLauncher
    WorkflowLauncher --> WorkflowCatalog
    WorkflowLauncher --> WorkflowStatusPublisher
    WorkflowLauncher --> RunContextStore
    WorkflowCatalogLoader --> WorkflowCatalog
```

---

## 3. データフロー

### 3.1 DLL ロードフロー

アプリケーション起動時にカスタムアクティビティ DLL を動的にロードし、Elsa に登録します。

```mermaid
sequenceDiagram
    autonumber
    participant Host as ホストアプリケーション
    participant Hosted as ActivityLoaderHostedService
    participant Loader as DirectoryActivityAssemblyLoader
    participant FS as ファイルシステム
    participant ALC as AssemblyLoadContext
    participant Module as IActivityModule
    participant Elsa as Elsa Runtime

    Host->>Hosted: StartAsync()
    Hosted->>Loader: LoadAndRegisterAsync()
    Loader->>FS: Activities/ ディレクトリスキャン<br/>*.dll パターン
    FS-->>Loader: DLL ファイル一覧
    
    loop 各 DLL ファイル
        Loader->>ALC: 新しい AssemblyLoadContext 作成<br/>(分離コンテキスト)
        Loader->>ALC: Assembly ロード
        ALC-->>Loader: Assembly
        
        alt IActivityModule 実装あり
            Loader->>Module: Activator.CreateInstance()
            Loader->>Module: RegisterAsync(ActivityModuleContext)
            Module->>Elsa: アクティビティ型登録
        else IActivityModule 実装なし
            Loader->>Elsa: IActivityRegistrar.RegisterExportedActivitiesAsync()
        end
    end
    
    Loader-->>Hosted: 完了
    Hosted-->>Host: 完了
```

### 3.2 ワークフローロードフロー

起動時に JSON ファイルからワークフロー定義を読み込み、カタログに登録・DB に永続化します。

```mermaid
sequenceDiagram
    autonumber
    participant Host as ホストアプリケーション
    participant Hosted as HostedService
    participant Loader as WorkflowCatalogLoader
    participant FS as ファイルシステム
    participant Catalog as WorkflowCatalog
    participant Elsa as Elsa Store

    Host->>Hosted: StartAsync()
    Hosted->>Loader: LoadAllAsync()
    Loader->>FS: docs/workflows/ スキャン<br/>workflow-*.json パターン
    FS-->>Loader: JSON ファイル一覧
    
    loop 各ワークフロー JSON
        Loader->>FS: JSON ファイル読み込み
        FS-->>Loader: JSON 文字列
        Loader->>Loader: デシリアライズ<br/>WorkflowGraph 構築
        Loader->>Loader: メタデータ設定<br/>(TaskId, 名前, 説明)
        Loader->>Catalog: Set(taskId, WorkflowCatalogEntry)
        Loader->>Elsa: SaveAsync(WorkflowDefinition)
    end
    
    Loader-->>Hosted: 完了
    Hosted-->>Host: 完了
```

### 3.3 ワークフロー実行フロー

MassTransit 経由でメッセージを受信し、ワークフローを実行、ステータスを通知します。

```mermaid
sequenceDiagram
    autonumber
    participant MQ as RabbitMQ
    participant MT as MassTransit
    participant Consumer as StartWorkflowConsumer
    participant Launcher as WorkflowLauncher
    participant Catalog as WorkflowCatalog
    participant Context as RunContextStore
    participant Runner as IWorkflowRunner
    participant Publisher as WorkflowStatusPublisher
    participant External as 外部システム

    MQ->>MT: StartWorkflowCommand メッセージ
    MT->>Consumer: Consume(context)
    
    Consumer->>Consumer: バリデーション<br/>(TaskId, RunTaskId 必須)
    Consumer->>Catalog: TryGet(taskId)
    Catalog-->>Consumer: WorkflowCatalogEntry
    
    Consumer->>Context: 重複チェック<br/>TryRegister(runTaskId, taskId)
    
    alt 重複なし
        Consumer->>Launcher: LaunchAsync(command)
        Launcher->>Publisher: PublishAsync(Running)
        Publisher->>External: ステータス通知
        
        Launcher->>Runner: RunAsync(workflowGraph, input)
        Runner-->>Launcher: WorkflowExecutionResult
        
        alt 実行成功
            Launcher->>Publisher: PublishAsync(Finished)
        else 実行エラー
            Launcher->>Publisher: PublishAsync(Error, detail)
        end
        
        Publisher->>External: ステータス通知<br/>(リトライ付き)
        Launcher->>Context: Unregister(runTaskId)
    else 重複あり
        Consumer->>Consumer: ログ出力・スキップ
    end
```

---

## 4. 技術スタック

| カテゴリ | 技術 | バージョン | 用途 |
|---------|------|-----------|------|
| **ランタイム** | .NET | 8.0 | アプリケーション実行基盤 |
| **ワークフローエンジン** | Elsa | 3.5.2 | ワークフロー設計・実行・管理 |
| **データベース** | PostgreSQL | - | ワークフロー定義・実行状態の永続化 |
| **メッセージキュー** | RabbitMQ | - | 非同期メッセージング |
| **メッセージング抽象化** | MassTransit | - | メッセージブローカー抽象化レイヤー |
| **フロントエンド** | Blazor WebAssembly | - | Elsa Studio GUI |
| **ORM** | Entity Framework Core | - | PostgreSQL アクセス |

### 設定値（デフォルト）

| 項目 | 設定値 |
|------|--------|
| PostgreSQL 接続先 | `Host=localhost;Port=5432;Database=elsa_workflows` |
| RabbitMQ 接続先 | `localhost:5672` |
| ワークフローキュー名 | `workflow-start` |
| 同時処理数 | 8 |
| プリフェッチ数 | 16 |
| ステータス通知リトライ | 3回、2秒間隔 |
| ワークフロー定義ディレクトリ | `docs/workflows/` |
| カスタムアクティビティディレクトリ | `Activities/` |

---

## 5. テスト方針

### テスト種別と実行方法

| テスト種別 | フレームワーク | 対象 | 実行方法 |
|-----------|--------------|------|---------|
| **単体テスト** | xUnit + Moq + FluentAssertions | 各サービスクラス (`WorkflowLauncher`, `WorkflowCatalog` 等) | `dotnet test --filter "Category!=E2E"` |
| **結合テスト** | xUnit + MassTransit.Testing | MassTransit パイプライン (`StartWorkflowConsumer` 等) | `dotnet test --filter "Category!=E2E"` |
| **E2E テスト** | xUnit + Microsoft.Playwright | Elsa Studio GUI 操作 | `dotnet test --filter "Category=E2E"` |

### テスト戦略

```mermaid
graph TB
    subgraph "単体テスト"
        UT1["WorkflowLauncher テスト"]
        UT2["WorkflowCatalog テスト"]
        UT3["WorkflowStatusPublisher テスト"]
        UT4["DirectoryActivityAssemblyLoader テスト"]
    end

    subgraph "結合テスト"
        IT1["StartWorkflowConsumer<br/>メッセージ処理テスト"]
        IT2["ワークフロー実行パイプライン<br/>テスト"]
    end

    subgraph "E2E テスト"
        E2E1["Elsa Studio<br/>ワークフロー設計テスト"]
        E2E2["ワークフロー実行<br/>エンドツーエンドテスト"]
    end

    UT1 --> IT1
    UT2 --> IT1
    UT3 --> IT2
    IT1 --> E2E2
    IT2 --> E2E2
```

### テスト観点

| テスト種別 | 主な観点 |
|-----------|---------|
| **単体テスト** | メソッド単位の動作検証、境界値テスト、例外処理、モック活用 |
| **結合テスト** | MassTransit メッセージフロー、コンシューマ連携、サービス間連携 |
| **E2E テスト** | ユーザー操作シナリオ、GUI 動作検証、ワークフロー実行確認 |

---

## 6. 主要クラス詳細

### 6.1 Activities.Contracts

#### IActivityModule

カスタムアクティビティ DLL が実装するエントリポイントインターフェース。

```csharp
public interface IActivityModule
{
    ValueTask RegisterAsync(ActivityModuleContext context, CancellationToken cancellationToken);
}
```

#### ActivityModuleContext

`IActivityModule.RegisterAsync()` に渡されるコンテキスト。DI コンテナへのアクセスを提供。

```csharp
public sealed record ActivityModuleContext(IServiceProvider Services);
```

#### ActivityModuleMetadataAttribute

アセンブリレベルで適用するメタデータ属性。

```csharp
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ActivityModuleMetadataAttribute : Attribute
{
    public ActivityModuleMetadataAttribute(string name, string version);
    public string? Description { get; init; }
}
```

### 6.2 Activities.Loader

#### DirectoryActivityAssemblyLoader

DLL 動的ロードの中核クラス。

| メソッド | 説明 |
|---------|------|
| `LoadAndRegisterAsync()` | 指定ディレクトリから DLL を検索・ロード・登録 |

**特徴:**
- 各 DLL を独立した `AssemblyLoadContext` でロード（分離）
- `Resolving` イベントで依存関係を解決
- `IActivityModule` 実装があれば優先的に使用
- パストラバーサル攻撃を防止

### 6.3 ElsaServer/Services

#### WorkflowCatalog

ワークフロー定義のインメモリレジストリ。

| メソッド | 説明 |
|---------|------|
| `Set(taskId, entry)` | ワークフローエントリを登録 |
| `TryGet(taskId, out entry)` | TaskId でワークフローを取得 |
| `List()` | 全ワークフロー一覧を取得 |

#### WorkflowLauncher

ワークフロー実行のオーケストレーター。

| メソッド | 説明 |
|---------|------|
| `LaunchAsync(command)` | `StartWorkflowCommand` からワークフローを実行 |

**処理フロー:**
1. TaskId/RunTaskId のバリデーション
2. カタログからワークフロー検索
3. 実行コンテキスト登録
4. ペイロードをワークフロー入力にマッピング
5. `IWorkflowRunner` で実行
6. ステータス通知（成功/エラー）

#### WorkflowStatusPublisher

ワークフロー実行ステータスの通知を担当。

| メソッド | 説明 |
|---------|------|
| `PublishAsync(taskId, runTaskId, status, detail)` | ステータス更新を通知 |
| `PublishWithRetryAsync()` | リトライ付きで通知 |

**ステータス種別:** `Running`, `Suspended`, `Error`, `Finished`

### 6.4 ElsaServer/Consumers

#### StartWorkflowConsumer

MassTransit コンシューマ。`StartWorkflowCommand` メッセージを処理。

**メッセージ構造:**

```csharp
public record StartWorkflowCommand
{
    public string TaskId { get; init; }
    public string RunTaskId { get; init; }
    public Dictionary<string, object?> Payload { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}
```

**処理フロー:**
1. ロギング
2. TaskId/RunTaskId バリデーション
3. カタログ存在確認
4. 重複実行チェック
5. ワークフロー実行
6. エラーハンドリング・ステータス通知

---

## 7. 設定オプション

### WorkflowBusOptions

MassTransit/RabbitMQ 設定。

| プロパティ | 説明 | デフォルト |
|-----------|------|-----------|
| `QueueName` | ワークフロー起動キュー名 | `workflow-start` |
| `ConcurrentMessageLimit` | 同時処理メッセージ数 | 8 |
| `PrefetchCount` | プリフェッチ数 | 16 |

### WorkflowCatalogOptions

ワークフローカタログ設定。

| プロパティ | 説明 | デフォルト |
|-----------|------|-----------|
| `Directory` | ワークフロー JSON ディレクトリ | `docs/workflows` |
| `Strict` | 厳格モード | true |

### ActivityLoaderOptions

アクティビティローダー設定。

| プロパティ | 説明 | デフォルト |
|-----------|------|-----------|
| `Directory` | DLL ディレクトリ | `Activities` |
| `SearchPattern` | 検索パターン | `*.dll` |
| `Recursive` | 再帰検索 | false |
| `Strict` | 厳格モード | false |
