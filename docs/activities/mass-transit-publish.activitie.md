# アクティビティ設計書: MassTransit Publish (プレースホルダ)

## 目的
ワークフロー内でMassTransit Publishを行い、RunTaskIdとStatusを外部へ通知する専用アクティビティを定義する（今後の拡張用）。

## 入出力
- 入力
  - `RunTaskId` (string, required)
  - `Status` (enum: Running|Suspended|Finished|Error, required)
  - `Detail` (string, optional)
- 出力
  - なし（Publishの結果は例外で通知）

## 振る舞い
- MassTransitの`IPublishEndpoint`を用いて`WorkflowStatusEvent`互換メッセージをPublish。
- 失敗時は例外送出し、上位でErrorハンドリング。

## 配置/ビルド
- テンプレートプロジェクトを`Activities/`配下に複製して実装し、`*.dll`をビルドして`Activities/`直下へ配置する。

## テスト
- DLLロード試験（Strict=true/false）。
- 入力の必須チェック（RunTaskId, Status）。
- Publishが呼ばれることのモック検証。
- 例外時にError Outcomeへ遷移すること。

## 備考
- 現状はプレースホルダ。実装後にJSONワークフローへ組み込み、サンプルWFもPublishアクティビティ経路へ更新する。
