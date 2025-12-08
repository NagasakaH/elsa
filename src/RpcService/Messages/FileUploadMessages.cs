using System.ComponentModel;

namespace NagasakaEventSystem.RpcService.Messages;

/// <summary>
/// ファイルアップロードリクエスト
/// </summary>
public class UploadFileRequest
{
    [DisplayName("ファイル名")]
    [Description("アップロードするファイルの名前")]
    public string FileName { get; set; } = string.Empty;

    [DisplayName("コンテンツタイプ")]
    [Description("ファイルのMIMEタイプ")]
    public string ContentType { get; set; } = string.Empty;

    [DisplayName("ファイルデータ")]
    [Description("Base64エンコードされたファイルデータ")]
    public string FileDataBase64 { get; set; } = string.Empty;

    [DisplayName("保存先フォルダ")]
    [Description("ファイルを保存するフォルダパス")]
    public string DestinationFolder { get; set; } = string.Empty;

    [DisplayName("メタデータ")]
    [Description("ファイルに関連付けるメタデータ")]
    public FileMetadata? Metadata { get; set; }

    [DisplayName("上書き許可")]
    [Description("同名ファイルが存在する場合に上書きするか")]
    public bool AllowOverwrite { get; set; } = false;
}

/// <summary>
/// ファイルメタデータ
/// </summary>
public class FileMetadata
{
    [DisplayName("タイトル")]
    public string? Title { get; set; }

    [DisplayName("説明")]
    public string? Description { get; set; }

    [DisplayName("タグ")]
    public List<string> Tags { get; set; } = new();

    [DisplayName("所有者ID")]
    public string? OwnerId { get; set; }

    [DisplayName("公開設定")]
    public FileVisibility Visibility { get; set; } = FileVisibility.Private;
}

/// <summary>
/// ファイル公開設定
/// </summary>
public enum FileVisibility
{
    Private,
    Internal,
    Public
}

/// <summary>
/// ファイルアップロードレスポンス
/// </summary>
public class UploadFileResponse
{
    [DisplayName("成功")]
    public bool Success { get; set; }

    [DisplayName("ファイルID")]
    public string FileId { get; set; } = string.Empty;

    [DisplayName("ファイルURL")]
    public string FileUrl { get; set; } = string.Empty;

    [DisplayName("ファイルサイズ")]
    public long FileSizeBytes { get; set; }

    [DisplayName("アップロード日時")]
    public DateTime UploadedAt { get; set; }

    [DisplayName("エラーメッセージ")]
    public string? ErrorMessage { get; set; }
}
