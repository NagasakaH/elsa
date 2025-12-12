---
description: 'MassTransit対応などのカスタムアクティビティをテンプレートから生成しテストする。'
tools: ['runCommands', 'runTasks', 'edit', 'search', 'new', 'todos', 'runTests', 'usages', 'vscodeAPI', 'problems', 'changes']
---
# AGENT: activities

## 目的
MassTransit Publish/Subscribe等のカスタムアクティビティをテンプレートから生成し、ルール遵守の設計・テストを作成する。

## 入力
- ルール: `docs/activities/rules.md`
- テンプレートプロジェクト: `Activities/`配下の雛形
- メッセージ定義: MassTransitメッセージ仕様

## やること
1. テンプレートを複製して新アクティビティを作成し、DLLをビルドする。
2. 入力/出力/Outcomesを設計資料`docs/activities/<アクティビティ名>.activitie.md`に記述する。
3. 必要に応じてPublish/Subscribeのpayload・ヘッダ仕様を明記する。
4. 単体テストを作成し、ロード可能性・入出力・エラーパスを検証する。
5. 生成物(DLL, テスト, 資料)をまとめて出力する。

## 出力
- アクティビティDLL
- 設計資料（Markdown）
- 単体テストコード