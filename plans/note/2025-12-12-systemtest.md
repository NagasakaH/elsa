# 2025-12-12 システムテスト手順メモ

- 前提: `docker compose up -d`でPostgreSQL/RabbitMQ起動。`dotnet run --project src/ElsaServer/ElsaServer.csproj`でStartWorkflowConsumerバインドと`Workflow loaded: sample-start`を確認。
- キュー: 起動受信 `workflow-start`。ステータスPublishはMassTransitの`workflow-status-event`エクスチェンジ（既定トポロジ）。
- 受信用一時キュー例:
  - `rabbitmqadmin declare queue name=workflow-status-test durable=false`
  - `rabbitmqadmin declare binding source=workflow-status-event destination=workflow-status-test routing_key=""`
- 起動要求送信例:
  - `rabbitmqadmin publish exchange="" routing_key="workflow-start" properties='{"content_type":"application/json","message_type":["urn:message:ElsaServer.Messages:StartWorkflowCommand"]}' payload='{"taskId":"sample-start","runTaskId":"run-001","payload":{},"headers":{}}'`
- 期待: `workflow-status-test`で`WorkflowStatusEvent` Running → Finished を順序取得（taskId=sample-start, runTaskId=run-001）。
- 異常確認:
  - TaskId未知: taskId=unknown-task → Error + detail "Unknown TaskId"。
  - RunTaskId重複: runTaskId同一で2回送信 → 2回目が Error + detail "Duplicated RunTaskId"。
- 自動化方針: MassTransit InMemory TestHarnessでStartWorkflowConsumer→WorkflowStatusPublisherを観測し、Running/Finished/Error発火をアサート。
