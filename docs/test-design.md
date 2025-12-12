# テスト設計書

## テスト対象
- 起動時ワークフロー読み込み（JSONルール適合性、命名規約）。
- MassTransit経由のワークフロー起動（TaskId/RunTaskIdハンドリング）。
- 状態通知Publish（Running/Suspended/Finished/Error）。
- Activities DLLロードと動作確認。
- サンプルワークフローを用いたシステムテスト。

## テスト方針
- 単体テスト: ロード/検証サービス、TaskId解決、RunTaskId管理、状態通知発火を対象。
- 結合テスト: MassTransit受信ハンドラとElsa起動の連携、状態Publish経路。
- システムテスト: サンプル`workflow-<TaskId>.json`を用い、メッセージ送信→状態受信までを確認。
- ルール準拠テスト: `docs/workflows/rules.md`と`docs/activities/rules.md`に対する検証ケースを用意。

## テスト観点と代表ケース
1. **起動時読み込み**
   - 正常: 正しい命名とスキーマのJSONがすべて登録される。
   - 異常: 命名不正、必須フィールド欠落、ステータス通知アクティビティ欠落。
2. **起動要求受付**
   - 既知TaskId+新規RunTaskIdで起動し`Running`がPublishされる。
   - 未知TaskIdで受信し`Error`がPublishされる。
   - RunTaskId重複時の扱い（上書き/拒否の仕様に沿う）。
3. **状態通知**
   - 正常終了で`Finished`がPublishされる。
   - 例外発生で`Error`がPublishされ、詳細が含まれる。
   - サスペンドパスで`Suspended`がPublishされる。
4. **Activities DLLロード**
   - テンプレート生成DLLが起動時にロードされる。
   - アクティビティの入力→期待出力が一致する。
5. **システムテスト**
   - サンプルワークフローに対し、MassTransitメッセージ送信→状態通知受信まで通る。

## テストデータ/環境
- ワークフローJSON配置: `docs/workflows/`
- サンプルJSON: `workflow-<TaskId>.json`（サンプル用TaskIdを定義）
- Activities DLL: テンプレートからビルドしたDLLを`Activities/`配下に配置。
- メッセージ送信: MassTransitのテストハーネスまたはインメモリバスを使用。

## 受入基準
- 上記観点の正常ケースがすべてPass。
- 異常系で期待通りにError/SuspendedがPublishされ、ログに記録される。
- サンプルシナリオのエンドツーエンドが完了する。