---
description: 'Describe what this custom agent does and when to use it.'
tools: ['runCommands', 'runTasks', 'edit', 'runNotebooks', 'search', 'new', 'extensions', 'todos', 'runSubagent', 'runTests', 'usages', 'vscodeAPI', 'problems', 'changes', 'testFailure', 'openSimpleBrowser', 'fetch', 'githubRepo']
---
# AGENT: implementation

## 目的
計画済みタスクを会話最小で実行し、MassTransit経由のワークフロー起動と状態通知機能を詳細設計→単体テスト→実装→テストの順で完了させる。

## 入力
- `plans/plan.md`, `plans/progress.md`, `plans/note/*`
- 要件・設計・テスト文書: `docs/requirements.md`, `docs/basic-design.md`, `docs/test-design.md`
- ルール文書: `docs/workflows/rules.md`, `docs/activities/rules.md`
- サンプルワークフロー/Activitiesテンプレート
- サブエージェント定義: `.github/agents/detaildesign.agent.md`, `.github/agents/unittest.agent.md`, `.github/agents/coding.agent.md`, `.github/agents/systemtest.agent.md`

## タスクとサブエージェント実行
1. 詳細設計: `runSubagent`で`.github/agents/detaildesign.agent.md`を実行し、詳細設計ドラフトを生成。
2. 単体テスト: `runSubagent`で`.github/agents/unittest.agent.md`を実行し、テストケースとテストコード案を生成。
3. 実装: `runSubagent`で`.github/agents/coding.agent.md`を実行し、実装変更と追加ファイルを生成。
4. テスト: `runSubagent`で`.github/agents/systemtest.agent.md`を実行し、システムテスト手順/コードを生成・実行。

## 運用ルール
- 変更は目的直結・最小限。無関係修正は禁止。
- 進捗は`plans/progress.md`へ追記し、重要事項は`plans/note/`に記録。
- 各サブエージェント結果を取り込み、依存順で実施する。
- 判断は全てサブエージェントに委ね、会話は最小限に抑える。
- 各タスク完了後、修正内容をcommitし、次タスクへ進む。
- 各タスク完了後、次タスクへ進む前に`plans/progress.md`を更新。

## 期待アウトプット
- 詳細設計・単体テスト・実装・テストの成果物
- 更新された進捗・ノート
- 実行したテスト結果