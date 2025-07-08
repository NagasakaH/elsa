# Subscribe Service

ElsaStudioから送信されたメッセージをログに出力する専用のコンソールアプリケーションです。

## 機能

- RabbitMQからメッセージを継続的に受信
- 受信したメッセージをコンソールとログファイルに出力
- 構造化ログ（JSON形式）でメッセージの詳細情報を記録
- Graceful shutdown対応

## 実行方法

### 前提条件

1. RabbitMQサーバーが起動していること（docker-compose使用推奨）
2. .NET 8 SDKがインストールされていること

### RabbitMQの起動

```bash
# プロジェクトルートディレクトリで実行
docker-compose up -d rabbitmq
```

### アプリケーションの実行

```bash
# Subscribeプロジェクトディレクトリで実行
cd src/Subscribe
dotnet run
```

または、プロジェクトルートから：

```bash
dotnet run --project src/Subscribe
```

## 設定

### appsettings.json

RabbitMQの接続設定とログ設定を `appsettings.json` で変更できます：

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "Default",
    "QueueName": "SubscribeQueue"
  }
}
```

### ログ出力

- **コンソール**: リアルタイムでメッセージを表示
- **ログファイル**: `logs/messages-YYYY-MM-DD.log` に構造化ログを出力

## メッセージフロー

1. ElsaStudio → ワークフロー作成・実行
2. ElsaServer → PublishMessageアクティビティ実行
3. RabbitMQ → メッセージキューイング
4. **Subscribe** → メッセージ受信・ログ出力

## 停止方法

`Ctrl+C` でGraceful shutdownが実行されます。
