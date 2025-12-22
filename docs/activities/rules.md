# アクティビティ作成ルール

## 配置・作成方法
- テンプレートプロジェクトを複製して作成し、ビルドでDLLを出力する。
- 出力DLLは`Activities/`配下に配置し、起動時にロードされる。

## 命名・資料
- 設計資料: `docs/activities/<アクティビティ名>.activitie.md`
- 活動名は用途が分かる名前を付与。

## 動作要件
- 入力(Input)と出力(Output)を明確に定義する。
- 内部エラーが想定される場合は、エラー用Outcomesを定義する。
- MassTransitメッセージ定義から生成する場合、Publish/Subscribe動作を明示し、必須ヘッダ・payloadをドキュメントに記載する。

## 品質ルール
- Elsaにアクティビティとしてロード可能であること（必要な属性・登録を実装）。
- 入力に対し期待する出力/動作を単体テストで保証する。
- 不要な外部依存を含めない。

## テスト
- DLLロードテスト: 起動時にロードされること。
- 機能テスト: 入力→出力が期待通りになること。
- エラーパス: 定義したエラーOutcomesが発火すること。

### アクティビティ単体テストのテンプレ
- テスト基盤: `tests/Activities.Testing/`（submodulesの `Elsa.Testing.Shared` を参考に、リポジトリ内で完結する最小構成のフィクスチャを用意）
- サンプル: `tests/Activities.Templates.CustomActivityTemplate.UnitTests/`

#### テストを書き始める手順
1. アクティビティプロジェクトを追加（例: `src/Activities.Templates/...`）
2. テストプロジェクトを追加し、以下を参照
	- `tests/Activities.Testing/Activities.Testing.csproj`
	- 対象アクティビティの `*.csproj`
3. `ActivityTestFixture` を使って実行し、`Journal.ActivityExecutionContexts` から出力を検証

#### 実行コマンド
```bash
dotnet test ./NagasakaEventSystem.sln
```

## サンプル
- テンプレートプロジェクトの例を`Activities/`配下に配置し、Publish/Subscribeアクティビティの雛形を提供する。