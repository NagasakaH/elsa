# 進捗記録

- 2025-12-12: 初期計画作成、前提確認、計画ディレクトリ構築。
- 2025-12-12: MassTransit起動/状態通知の詳細設計ドラフト更新（`docs/detail-design.md`追加、メッセージ契約・フロー・設定整理）。
- 2025-12-12: 単体テストケース案作成（Validator/RunContextStore/PayloadMapper/Consumer/StatusPublisher）。
- 2025-12-12: 実装着手（StartWorkflowConsumer/Launcher、StatusPublisher、カタログ/アクティビティローダー、設定DI、サンプルWF配置）。
- 2025-12-12: システムテスト手順案策定（RabbitMQ経由E2E、正常/異常系、TestHarness案）。
- 2025-12-12: 単体/結合/システムのテスト計画ドラフト作成（tests/test-plan.md）。
- 2025-12-12: サンプルワークフローJSON(`workflow-sample-basic-flow.json`)と設計書(`sample-basic-flow.workflow.md`)、MassTransit Publishアクティビティ設計プレースホルダを追加。
- 2025-12-12: PublishWorkflowStatusアクティビティ実装とDI登録、サンプルWFへRunning/Finished通知経路を組み込み、ビルド実行。
- 2025-12-12: 詳細設計追加ドラフトをplans/noteに追記（Strict挙動、メッセージ契約、サンプルWF案、テスト観点）。
- 2025-12-12: ElsaServer.UnitTestsプロジェクト追加、RunContextStore/PayloadMapper/WorkflowLauncher/StatusPublisher/StartWorkflowConsumerの単体テスト実装、ソリューション登録。
- 2025-12-12: Program.csの活動登録をAddElsaチェーンに修正、`dotnet test NagasakaEventSystem.sln` でユニットテスト11件が成功。
- 2025-12-12: StartWorkflowIntegrationTestsで MassTransit InMemory 受信エンドポイントとロガー登録を追加し、Running/Finished のステータスイベント確認までパス。