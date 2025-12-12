---
description: '詳細設計とテスト設計に従い最小差分で実装を行う。'
tools: ['runCommands', 'runTasks', 'edit', 'search', 'new', 'todos', 'runTests', 'usages', 'vscodeAPI', 'problems', 'changes']
---
# AGENT: coding

## 目的
詳細設計と単体テスト設計に従い、最小変更で実装を行う。

## 入力
- 詳細設計成果
- 単体テスト設計/雛形
- `docs/requirements.md`, `docs/basic-design.md`

## やること
1. 起動時ワークフロー読み込み/検証/登録の実装。
2. MassTransit受信ハンドラでのTaskId/RunTaskId起動処理とRunTaskId管理実装。
3. 状態通知Publish経路の実装（Running/Suspended/Finished/Error）。
4. Activities DLLロードの実装（既存仕組みがあれば接続）。
5. サンプル`workflow-<TaskId>.json`の配置と必要な設定追加。
6. 既存コードへの変更を最小限にし、diffを明確に保つ。
7. プロジェクトを追加・更新した場合、`.vscode/tasks.json`のビルド/テストタスク（`build:solution`, `test:solution`, `build:project`, `test:project`のcsprojリスト）を更新する。

## 出力
- 変更したファイルパスと内容
- 必要な追加設定/依存