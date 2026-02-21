# Elsa Workflow System

Elsa Workflow Engine を拡張したワークフロー管理システムです。Activity DLL の動的ロード、JSON ワークフロー管理、MassTransit 連携、Elsa Studio による GUI 操作をサポートします。

---

## 📚 ドキュメント

詳細なドキュメントは [docs/README.md](./docs/README.md) を参照してください。

| カテゴリ | ドキュメント |
|---------|-------------|
| 要件定義 | [docs/01-requirements/](./docs/01-requirements/) |
| 基本設計 | [docs/02-basic-design/](./docs/02-basic-design/) |
| 詳細設計 | [docs/03-detailed-design/](./docs/03-detailed-design/) |
| テスト仕様 | [docs/04-test-specification/](./docs/04-test-specification/) |
| 開発ガイド | [docs/development-guide.md](./docs/development-guide.md) |
| アーキテクチャ | [docs/architecture.md](./docs/architecture.md) |

---

## 🔧 技術スタック

| カテゴリ | 技術 |
|---------|------|
| フレームワーク | .NET 8.0 |
| ワークフローエンジン | Elsa 3.5.2 |
| データベース | PostgreSQL |
| メッセージブローカー | RabbitMQ |
| メッセージング | MassTransit |
| フロントエンド | Blazor WebAssembly (Elsa Studio) |

---

## 🚀 セットアップ

### 前提条件

- .NET 8.0 SDK
- Docker / Docker Compose
- PostgreSQL（Docker で起動可能）
- RabbitMQ（Docker で起動可能）

### 1. インフラストラクチャの起動

```bash
# PostgreSQL, RabbitMQ を起動
docker compose up -d
```

### 2. ビルド

```bash
dotnet build
```

### 3. 実行

```bash
# Elsa Server の起動
cd src/ElsaServer
dotnet run
```

Elsa Studio は `https://localhost:5001` でアクセスできます。

---

## 🧪 テスト

### 全テスト実行（E2E除外）

```bash
dotnet test --filter "Category!=E2E"
```

### E2Eテストのみ実行

```bash
# サーバー起動後に実行
ELSA_BASE_URL=https://localhost:5001 dotnet test --filter "Category=E2E"
```

### chrome-novnc で E2E + スクリーンショット実行

```bash
# 1) Chrome + noVNC を起動（CDP:9222, noVNC:6080）
docker run -d --rm --name chrome-novnc \
  -p 6080:6080 -p 9222:9222 \
  vital987/chrome-novnc

# 2) E2E実行（スクリーンショット保存先を指定）
ELSA_BASE_URL=https://localhost:5001 \
E2E_CHROME_NOVNC_CDP_URL=http://127.0.0.1:9222 \
E2E_SCREENSHOT_DIR=./artifacts/e2e-screenshots \
dotnet test --filter "Category=E2E"
```

スクリーンショットは `E2E_SCREENSHOT_DIR`（未指定時: `bin/.../artifacts/screenshots`）に保存されます。

### カバレッジレポート生成

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 📁 プロジェクト構成

```
.
├── src/
│   ├── ElsaServer/           # メインサーバーアプリケーション
│   ├── ElsaServer.Activities/ # カスタムアクティビティ
│   └── ElsaServer.Common/    # 共通ライブラリ
├── tests/
│   ├── ElsaServer.Tests/     # 単体テスト・結合テスト
│   └── ElsaServer.E2E/       # E2Eテスト
├── docs/                     # ドキュメント
└── compose.yml               # Docker Compose 設定
```

---

## 📖 主な機能

### Activity DLL 動的ロード

外部 DLL からカスタムアクティビティを動的にロードし、ワークフローで使用できます。

### JSON ワークフロー管理

ワークフロー定義を JSON ファイルで管理し、起動時に自動インポートできます。

### MassTransit 連携

RabbitMQ を介したイベント駆動型ワークフロー実行をサポートします。

### Elsa Studio GUI

Blazor WebAssembly ベースの GUI でワークフローの作成・編集・監視が可能です。

---

## 🔗 関連リンク

- [Elsa Workflows 公式](https://v3.elsaworkflows.io/)
- [MassTransit 公式](https://masstransit.io/)

---

## ライセンス

MIT License
