# Elsa Studio Environment

.NET 8を使用したElsa Studio実行環境です。ワークフローの作成と実行が可能です。

## 前提条件

- .NET 8 SDK
- PostgreSQL 15以上（またはDockerを使用）
- RabbitMQ（オプション、メッセージング用）

## セットアップ手順

### 1. データベースの準備

#### Option A: Dockerを使用する場合
```bash
# Docker Composeをインストール（まだの場合）
sudo apt install docker-compose

# PostgreSQLとRabbitMQを起動
docker-compose up -d

# データベース接続確認
docker exec -it elsa-postgres psql -U elsa_user -d elsa_workflows
```

#### Option B: ローカルPostgreSQLを使用する場合
```bash
# PostgreSQLをインストール
sudo apt update
sudo apt install postgresql postgresql-contrib

# PostgreSQLサービス開始
sudo systemctl start postgresql
sudo systemctl enable postgresql

# データベースとユーザーを作成
sudo -u postgres psql
CREATE DATABASE elsa_workflows;
CREATE USER elsa_user WITH PASSWORD 'elsa_password';
GRANT ALL PRIVILEGES ON DATABASE elsa_workflows TO elsa_user;
\q
```

### 2. データベースマイグレーション

```bash
# Entity Framework Core ツールのPATHを設定
export PATH="$PATH:/home/haoming/.dotnet/tools"

# マイグレーションを作成
dotnet ef migrations add InitialCreate --project src/Elsa.Shared --startup-project src/Elsa.Studio.Host

# データベースを更新
dotnet ef database update --project src/Elsa.Shared --startup-project src/Elsa.Studio.Host
```

### 3. アプリケーションの起動

```bash
# 依存関係を復元
dotnet restore

# アプリケーションをビルド
dotnet build

# Elsa Studio Hostを起動
dotnet run --project src/Elsa.Studio.Host
```

### 4. アクセス

- **Elsa Studio**: https://localhost:5001
- **Elsa API**: https://localhost:5001/elsa/api
- **RabbitMQ Management** (Dockerを使用した場合): http://localhost:15672 (guest/guest)

## プロジェクト構造

```
elsa/
├── src/
│   ├── Elsa.Shared/              # 共有ライブラリ
│   │   ├── Persistence/          # データベース関連
│   │   └── Extensions/           # サービス拡張
│   └── Elsa.Studio.Host/         # Blazor Studio アプリケーション
├── tests/                        # テストプロジェクト
├── docs/                         # ドキュメント
├── docker-compose.yml            # Docker構成
└── README.md                     # このファイル
```

## 設定

### データベース接続文字列

`src/Elsa.Studio.Host/appsettings.json` でデータベース接続文字列を変更できます：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=elsa_workflows;Username=elsa_user;Password=elsa_password"
  }
}
```

### Elsa設定

- **ワークフロー管理**: Entity Framework Core使用
- **ワークフロー実行**: Entity Framework Core使用
- **アイデンティティ**: Entity Framework Core使用
- **JavaScript/Liquid**: サポート済み
- **HTTP/Email/Timer**: アクティビティサポート済み
- **Quartz**: スケジューラー統合済み
- **MassTransit**: メッセージング統合済み

## トラブルシューティング

### データベース接続エラー
1. PostgreSQLが起動していることを確認
2. 接続文字列の設定を確認
3. ファイアウォール設定を確認

### マイグレーションエラー
```bash
# マイグレーションをリセット
dotnet ef database drop --project src/Elsa.Shared --startup-project src/Elsa.Studio.Host
dotnet ef migrations remove --project src/Elsa.Shared --startup-project src/Elsa.Studio.Host
```

### ポート競合エラー
- `src/Elsa.Studio.Host/Properties/launchSettings.json` でポート番号を変更

## 開発コマンド

```bash
# ソリューション全体をビルド
dotnet build

# テスト実行
dotnet test

# 新しいマイグレーションを追加
dotnet ef migrations add <MigrationName> --project src/Elsa.Shared --startup-project src/Elsa.Studio.Host

# データベースを最新の状態に更新
dotnet ef database update --project src/Elsa.Shared --startup-project src/Elsa.Studio.Host
```

## 次のステップ

1. カスタムアクティビティの作成
2. ワークフロー定義の追加
3. 外部システムとの統合
4. セキュリティ設定の強化
5. 本番環境デプロイメントの準備
