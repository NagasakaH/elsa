using System.ComponentModel;
using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.ServiceBus.MassTransit.Builders;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Activities;

/// <summary>
/// ファイルアップロードアクティビティ
/// 
/// 方法2: 属性 + プロパティで詳細定義
/// - ネストしたオブジェクト（Metadata）をフラット化する例
/// - Base64データを扱う例
/// - コレクション（Tags）をJSON入力する例
/// </summary>
[RpcActivity(
    DisplayName = "ファイルアップロード",
    Description = "ファイルをストレージにアップロードします。メタデータやアクセス権限も設定できます。",
    Category = "ファイル管理",
    DestinationQueue = "UploadFile",
    TimeoutSeconds = 120,
    AutoGenerateInputs = false,
    AutoGenerateOutputs = false
)]
public class UploadFileActivity : RpcActivityDefinitionBase<UploadFileRequest, UploadFileResponse>
{
    // ========================================
    // ファイル情報（入力）
    // ========================================

    [RpcInput(
        DisplayName = "ファイル名",
        Description = "アップロードするファイルの名前（拡張子含む）",
        SourcePath = "FileName",
        Order = 1,
        IsRequired = true,
        Example = "document.pdf")]
    public string FileName { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "コンテンツタイプ",
        Description = "ファイルのMIMEタイプ",
        SourcePath = "ContentType",
        Order = 2,
        IsRequired = true,
        Example = "application/pdf")]
    public string ContentType { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "ファイルデータ(Base64)",
        Description = "Base64エンコードされたファイルデータ",
        SourcePath = "FileDataBase64",
        Order = 3,
        UIHint = "multiline",
        IsRequired = true)]
    public string FileDataBase64 { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "保存先フォルダ",
        Description = "ファイルを保存するフォルダパス",
        SourcePath = "DestinationFolder",
        Order = 4,
        IsRequired = true,
        Example = "/uploads/documents/2025/01")]
    public string DestinationFolder { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "上書き許可",
        Description = "同名ファイルが存在する場合に上書きする",
        SourcePath = "AllowOverwrite",
        Order = 5,
        UIHint = "checkbox",
        DefaultValue = false)]
    public bool AllowOverwrite { get; set; } = false;

    // ========================================
    // メタデータ（ネストしたオブジェクトをフラット化）
    // ========================================

    [RpcInput(
        DisplayName = "タイトル",
        Description = "ファイルの表示タイトル（任意）",
        SourcePath = "Metadata.Title",
        Order = 10)]
    public string? MetadataTitle { get; set; }

    [RpcInput(
        DisplayName = "説明",
        Description = "ファイルの説明（任意）",
        SourcePath = "Metadata.Description",
        Order = 11,
        UIHint = "multiline")]
    public string? MetadataDescription { get; set; }

    [RpcCollectionInput(
        DisplayName = "タグ",
        Description = "ファイルに付けるタグのリスト。JSON形式で入力: [\"重要\", \"契約書\", \"2025年度\"]",
        SourcePath = "Metadata.Tags",
        Order = 12,
        UseJsonInput = true,
        Example = "[\"重要\", \"契約書\"]")]
    public List<string> MetadataTags { get; set; } = new();

    [RpcInput(
        DisplayName = "所有者ID",
        Description = "ファイルの所有者ユーザーID（任意）",
        SourcePath = "Metadata.OwnerId",
        Order = 13)]
    public string? MetadataOwnerId { get; set; }

    [RpcInput(
        DisplayName = "公開設定",
        Description = "ファイルの公開範囲（Private, Internal, Public）",
        SourcePath = "Metadata.Visibility",
        Order = 14,
        UIHint = "dropdown")]
    public FileVisibility MetadataVisibility { get; set; } = FileVisibility.Private;

    // ========================================
    // アップロード結果（出力）
    // ========================================

    [RpcOutput(
        DisplayName = "成功",
        Description = "アップロードが成功したかどうか",
        SourcePath = "Success",
        Order = 1)]
    public bool Success { get; set; }

    [RpcOutput(
        DisplayName = "ファイルID",
        Description = "作成されたファイルの一意識別子",
        SourcePath = "FileId",
        Order = 2)]
    public string FileId { get; set; } = string.Empty;

    [RpcOutput(
        DisplayName = "ファイルURL",
        Description = "ファイルにアクセスするためのURL",
        SourcePath = "FileUrl",
        Order = 3)]
    public string FileUrl { get; set; } = string.Empty;

    [RpcOutput(
        DisplayName = "ファイルサイズ(バイト)",
        Description = "アップロードされたファイルのサイズ",
        SourcePath = "FileSizeBytes",
        Order = 4)]
    public long FileSizeBytes { get; set; }

    [RpcOutput(
        DisplayName = "アップロード日時",
        Description = "ファイルがアップロードされた日時",
        SourcePath = "UploadedAt",
        Order = 5)]
    public DateTime UploadedAt { get; set; }

    [RpcOutput(
        DisplayName = "エラーメッセージ",
        Description = "エラーが発生した場合のメッセージ",
        SourcePath = "ErrorMessage",
        Order = 10)]
    public string? ErrorMessage { get; set; }
}
