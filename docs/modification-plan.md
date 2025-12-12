# 修正計画

## 方針
- 会話回数を減らし、生成AIエージェントを活用して集約的に作業する。
- plans/配下で計画・進捗・ノートを管理し、依存関係に従って実施する。
- 実装順序は「詳細設計書→単体テスト→実装→テスト」を厳守。

## タスク一覧と依存関係
1. 要件・基本設計・テスト設計ドラフト作成（完了済み）
2. ルール文書作成（ワークフロー/アクティビティ）（依存: 1）
3. 生成AIエージェント定義（workflowdesign, activities, implementation）（依存: 2）
4. 詳細設計（ElsaServerロード/起動/通知/サンプルJSON仕様）（依存: 2）
5. 単体テスト作成（ロード検証、TaskId解決、RunTaskId管理、状態通知）（依存: 4）
6. 実装（起動時ロード、起動ハンドラ、状態Publish、サンプルJSON配置）（依存: 5）
7. システムテスト実施（MassTransit経由起動と通知確認）（依存: 6）
8. ドキュメント・進捗更新（継続）

## 成果物
- `docs/requirements.md`, `docs/basic-design.md`, `docs/test-design.md`
- `docs/workflows/rules.md`, `docs/activities/rules.md`
- `.github/agents/workflowdesign.agent.md`, `.github/agents/activities.agent.md`, `.github/agents/implementation.agent.md`
- `plans/plan.md`, `plans/progress.md`, `plans/note/*.md`
- サンプル`workflow-<TaskId>.json`

## 実施上の注意
- 既存`docs/`構成は変更せず追記で対応。
- ElsaStudioのUI改修は対象外。
- 状態通知はMassTransit Publishのみ。
- 後方互換要求なし。必要に応じてJSONを更新。