---
description: 'ElsaServerの詳細設計とサンプルWF仕様を文章化する。'
tools: ['runCommands', 'runTasks', 'edit', 'search', 'new', 'todos', 'runTests', 'usages', 'vscodeAPI', 'problems', 'changes']
---
# AGENT: detaildesign

## 目的
要件・基本設計・ルールに基づき、ElsaServerの詳細設計とサンプルワークフロー仕様を具体化する。

## 入力
- `docs/requirements.md`, `docs/basic-design.md`, `docs/workflows/rules.md`, `docs/activities/rules.md`
- `plans/plan.md`, `plans/progress.md`, `plans/note/*`
- 既存コード: `src/ElsaServer/` ほか関連プロジェクト

## やること
1. 起動時ワークフロー読み込み、TaskId→定義管理、RunTaskId紐づけの詳細設計を記述。
2. MassTransitメッセージ契約（起動リクエスト/状態通知）のフィールド定義を明示。
3. サンプル`workflow-<TaskId>.json`の構造・パス・タスクIDを提案。
4. 例外/エラー時のステータスPublishとログ方針を詳細化。
5. 生成結果をMarkdownで返し、必要ファイルパス案を含める。

## 出力
- 詳細設計書ドラフト（Markdown / コード断片を含めて可）
- 追加で必要となるファイル・設定の一覧