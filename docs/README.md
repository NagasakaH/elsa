# Elsa Workflow System ドキュメントインデックス

このドキュメントは、Elsa Workflow System の技術ドキュメント一覧です。

## ドキュメント構成

```
docs/
├── README.md                          # このファイル（ドキュメントインデックス）
├── 01-requirements/
│   └── requirements-definition.md     # 要件定義書
├── 02-basic-design/
│   └── basic-design.md                # 基本設計書
├── 03-detailed-design/
│   └── detailed-design.md             # 詳細設計書
├── 04-test-specification/
│   └── test-specification.md          # テスト仕様書
├── architecture.md                    # アーキテクチャ概要
├── api-design.md                      # API設計書
├── configuration.md                   # 設定リファレンス
├── custom-activities.md               # カスタムアクティビティ開発ガイド
├── database-design.md                 # データベース設計書
├── development-guide.md               # 開発ガイド
├── implementation-plan.md             # 実装計画
├── elsa-studio-import-json.md         # Elsa Studio JSONインポートガイド
├── startup-import-and-run.md          # 起動時インポート・実行ガイド
├── workflow-definition-schema.jsonc   # ワークフロー定義スキーマ
└── workflows/
    ├── rules.md                       # ワークフロー定義ルール
    ├── samples/                       # サンプルワークフロー
    └── workflow-sample-basic-flow.json
```

---

## 📋 設計ドキュメント（開発フェーズ順）

### 1. 要件定義

| ドキュメント | 説明 |
|-------------|------|
| [要件定義書](./01-requirements/requirements-definition.md) | システム要件、機能要件、非機能要件の定義 |

### 2. 基本設計

| ドキュメント | 説明 |
|-------------|------|
| [基本設計書](./02-basic-design/basic-design.md) | システム全体のアーキテクチャ、コンポーネント構成 |
| [アーキテクチャ概要](./architecture.md) | システムアーキテクチャの詳細 |
| [データベース設計](./database-design.md) | データベーススキーマ設計 |
| [API設計](./api-design.md) | REST API エンドポイント設計 |

### 3. 詳細設計

| ドキュメント | 説明 |
|-------------|------|
| [詳細設計書](./03-detailed-design/detailed-design.md) | 各コンポーネントの詳細仕様 |
| [カスタムアクティビティ開発](./custom-activities.md) | Activity DLL の開発方法 |
| [設定リファレンス](./configuration.md) | 設定オプションの詳細 |

### 4. テスト仕様

| ドキュメント | 説明 |
|-------------|------|
| [テスト仕様書](./04-test-specification/test-specification.md) | 単体・結合・E2Eテスト仕様 |

---

## 📖 運用・開発ガイド

| ドキュメント | 説明 |
|-------------|------|
| [開発ガイド](./development-guide.md) | 開発環境セットアップ、コーディング規約 |
| [実装計画](./implementation-plan.md) | 実装フェーズの計画とタスク |
| [Elsa Studio JSONインポート](./elsa-studio-import-json.md) | GUI からのワークフローインポート手順 |
| [起動時インポート・実行](./startup-import-and-run.md) | アプリケーション起動時の自動インポート |

---

## 📁 ワークフロー定義

| ドキュメント | 説明 |
|-------------|------|
| [ワークフロー定義ルール](./workflows/rules.md) | JSON ワークフロー定義のルール |
| [ワークフロー定義スキーマ](./workflow-definition-schema.jsonc) | JSON Schema |
| [サンプル: 基本フロー](./workflows/workflow-sample-basic-flow.json) | 基本的なワークフロー例 |

---

## クイックリンク

- **プロジェクトルート**: [../README.md](../README.md)
- **ソースコード**: [../src/](../src/)
- **テストコード**: [../tests/](../tests/)
