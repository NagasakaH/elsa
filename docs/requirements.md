# 要件定義書

## 目的
MassTransit経由でTaskIdとRunTaskIdを指定してワークフローを起動し、状態をPublishで通知できるElsa実行環境を構築する。起動時にワークフローJSONを読み込み、サンプルを用いたシステムテストまでを可能にする。

## スコープ
- ElsaServerによるワークフローJSONの起動時読み込みとオンメモリ管理。
- TaskId/RunTaskId指定のワークフロー起動と状態通知（Publish）。
- ワークフロー/アクティビティ作成ルールと生成AIエージェントの整備。
- サンプルワークフロー/システムテストの準備。
- ElsaStudio UI改修は対象外。
- 修正計画と進捗管理（plans/配下）および実装実行エージェントの整備。

## 用語
- **TaskId**: ワークフローを一意に識別するID（1対1紐づけ）。
- **RunTaskId**: ワークフロー実行インスタンスの識別子。オンメモリで管理。
- **ワークフローJSON**: `workflow-<TaskId>.json` 形式で `docs/workflows/` 配下に配置。

## 機能要件
1. **起動時読み込み**: アプリ起動時に `docs/workflows/` 配下のJSONをすべて読み込み、TaskIdと定義をオンメモリ管理する。動的リロードは不要。
2. **起動メッセージ受付**: MassTransitで受信するメッセージから `TaskId`, `RunTaskId`（必須）を取得し、対応するワークフローを起動する。TaskIdが未登録の場合はエラーとして状態通知する。
3. **RunTaskId管理**: 受信メッセージ単位でRunTaskIdをインスタンスに紐づけオンメモリ保持する。
4. **状態通知**: ワークフローは必ずMassTransit Publishアクティビティで `RunTaskId` と `Status` を外部通知する。Statusは `Running`, `Suspended`, `Finished`, `Error` をサポート。
5. **ワークフロー設計資料**: 各ワークフローの設計資料を `docs/activities/<ワークフロー名>.workflow.md` に作成する。
6. **ワークフロールール**: JSON作成ルールを `docs/workflows/rules.md` にまとめ、生成AI用エージェント `.github/agents/workflowdesign.agent.md` を用意する。
7. **カスタムアクティビティ**: `Activities/` 配下DLLをロードして使用可能とし、作成ルールを `docs/activities/rules.md` にまとめ、生成AI用エージェント `.github/agents/activities.agent.md` を用意する。テンプレートプロジェクトの複製で作成し、テストを伴うこと。
8. **ワークフローJSON管理**: `workflow-<TaskId>.json` 形式でTaskIdと1対1に配置し、RunTaskIdは実行要求ごとに受領値を使用する。JSONスキーマ/ルールに従い必須フィールドと状態通知パスを組み込む。
9. **生成AIエージェント**: ワークフロー生成用 `.github/agents/workflowdesign.agent.md`、アクティビティ生成用 `.github/agents/activities.agent.md`、実装実行用 `.github/agents/implementation.agent.md` を整備し、会話回数を抑えて一括実行できるようにする。
10. **計画と進捗管理**: `plans/plan.md` に全体計画、`plans/progress.md` に進捗、`plans/note/*.md` に決定事項を記録する。
11. **システムテスト準備**: サンプルのワークフローJSONを登録し、MassTransit経由で起動・状態通知を受信するシステムテストを可能にする。
12. **実装順序遵守**: 実作業は「詳細設計書の作成 → 単体テストの作成 → 実装 → テスト」の順で実施する。

## 非機能要件
- **パフォーマンス**: 起動時の一括読み込みが実用的時間で完了すること（JSON数が増えても直列読込で許容）。
- **信頼性**: 読込・起動エラーはログおよび状態通知で検知可能にする。
- **運用**: デプロイ後の運用はJSON追加で対応。再起動で反映。
- **拡張性**: 活動・ワークフローはルールとテンプレートで拡張可能。

## 制約・対象外
- ElsaStudioのUI改修や追加画面は対象外。
- 状態通知チャネルはMassTransit Publishのみ。
- 後方互換要件はなし（必要に応じてJSON更新）。

## 受入条件
- 起動時にサンプルを含むワークフローJSONが読み込まれる。
- MassTransitメッセージにより指定TaskId/RunTaskIdのワークフローが起動する。
- 実行中に状態がPublishされ、RunTaskId単位で確認できる。
- ルール文書と生成AIエージェントが整備されている。
- システムテスト用シナリオが用意され、実行可能な状態になっている。
- 計画・進捗・ノートがplans配下で管理され、実装順序が遵守されている。