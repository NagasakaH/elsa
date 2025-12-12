# 作業計画

## 目的
- MassTransit経由でTaskId/RunTaskId指定のワークフロー起動と状態通知を実現し、起動時ワークフロー読み込みとサンプルを含むシステムテストまで整備する。

## スコープ
- ElsaServer側のロード・起動・通知処理。
- ワークフロー/アクティビティのルール整備と生成AIエージェント定義。
- 要件・基本設計・テスト設計・修正計画のドキュメント作成。
- サンプルワークフロー/アクティビティ設計資料とテスト方針（実装順序: 詳細設計→単体テスト→実装→テスト）。

## 前提
- ワークフローJSONは`docs/workflows/`配下で管理し、起動時一括読み込み。動的リロード不要。
- TaskIdとワークフローは1対1。RunTaskIdはインスタンス識別子でオンメモリ管理。
- 状態通知はMassTransit Publishのみ。ElsaStudio改修は対象外。
- ルールやドキュメントはすべて日本語で記述する。

## 作業項目と依存
1. 要件定義書・基本設計書・テスト設計書ドラフト作成（依存: なし）
2. ワークフロー/アクティビティのルール文書作成（依存: 1）
3. 生成AIエージェント定義（workflowdesign/activities/implementation）（依存: 2）
4. 詳細設計（ElsaServerの起動時ロード/TaskId-RunTaskId管理/状態Publish/サンプルJSON設計）（依存: 2）
5. 単体テスト設計・作成（依存: 4）
6. 実装（起動時ロード、MassTransit経由起動、状態通知、サンプルJSON配置）（依存: 5）
7. システムテスト（MassTransit経由で起動・状態受信を確認）（依存: 6）
8. 進捗・ノート更新（継続）

## 成果物
- `docs/requirements.md` 要件定義書
- `docs/basic-design.md` 基本設計書
- `docs/test-design.md` テスト設計書
- `docs/modification-plan.md` 修正計画
- `docs/workflows/rules.md` ワークフロールール
- `docs/activities/rules.md` アクティビティルール
- `.github/agents/workflowdesign.agent.md` ワークフロー生成AI用
- `.github/agents/activities.agent.md` アクティビティ生成AI用
- `.github/agents/implementation.agent.md` 実装実行用
- `plans/progress.md`, `plans/note/*.md`