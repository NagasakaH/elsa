---
description: 'MassTransit経由のE2Eシステムテスト手順/コードを生成・実行する。'
tools: ['runCommands', 'runTasks', 'edit', 'search', 'new', 'todos', 'runTests', 'usages', 'vscodeAPI', 'problems', 'changes']
---
# AGENT: systemtest

## 目的
MassTransit経由のエンドツーエンド動作をシステムテストで検証する。

## 入力
- 実装済みコード
- サンプル`workflow-<TaskId>.json`
- `docs/test-design.md`

## やること
1. MassTransitテストハーネス/実バスで起動メッセージ送信→状態通知受信を確認するシナリオを作成。
2. サンプルワークフローでRunning→Finished/Errored/Suspendedをカバー。
3. テストコードまたは手順書を生成し、期待結果を明示。

## 出力
- システムテスト手順/コード
- 期待されるログ/通知例