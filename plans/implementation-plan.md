# 実装計画

## 目的
MassTransit経由のTaskId/RunTaskId起動と状態Publishを、要求順序（詳細設計→単体テスト→実装→テスト）で完了させるための具体的な実行計画を示す。

## 前提/入力
- 要件・設計: `docs/requirements.md`, `docs/basic-design.md`, `docs/test-design.md`
- ルール: `docs/workflows/rules.md`, `docs/activities/rules.md`
- エージェント: `.github/agents/workflowdesign.agent.md`, `.github/agents/activities.agent.md`, `.github/agents/implementation.agent.md`
- 計画管理: `plans/plan.md`, `plans/progress.md`, `plans/note/*.md`

## タスクと依存
1. **詳細設計更新** (依存: 前提読了)
   - ElsaServerロード/起動/通知の詳細、JSON検証、RunContextStoreの仕様、ステータスPublish再試行を明文化。
   - サンプルワークフロー/アクティビティの仕様を決定し、`plans/note/`に記録。
2. **単体テスト作成** (依存: 1)
   - Loader/Validator/RunContextStore/StatusPublisher/Consumer/Mapperのテストコードを追加。
   - Activitiesテンプレートのロード・入出力・エラーパスのテストを追加。
3. **実装** (依存: 2)
   - 起動時カタログロード・Activitiesロード実装を確定（Strict設定含む）。
   - StartWorkflowConsumer/WorkflowLauncher/PayloadMapper/StatusPublisherの実装と配線。
   - サンプル `workflow-<TaskId>.json` と対応する設計資料 `docs/activities/<name>.workflow.md` を作成。
   - Activitiesテンプレートを複製してDLLを生成し配置。
4. **テスト** (依存: 3)
   - 単体テスト実行。
   - 結合テスト: MassTransit受信→Elsa実行→Publish確認。
   - システムテスト: サンプルワークフローを使い、RabbitMQ経由で起動し状態通知を確認。
5. **ドキュメント/進捗更新** (継続)
   - `plans/progress.md` に結果を追記。
   - 重要決定や調査は `plans/note/*.md` に記録。

## 実施順序と成果物
- 順序: 詳細設計 → 単体テスト → 実装 → テスト。
- 成果物: 設計更新、テストコード、実装コード、サンプルワークフローJSON、Activities DLL、テスト結果ログ、進捗/ノート更新。

## 実行時の指針
- 生成AIエージェントでタスクをバッチ処理し、会話回数を最小化。
- 無関係な修正を避け、要求に直結する範囲に限定。
- Strict設定時の読み込み失敗は検知し、エラー/警告方針を設定値に従わせる。
- 状態通知は必ずMassTransit PublishでRunTaskIdとStatusを含める。
