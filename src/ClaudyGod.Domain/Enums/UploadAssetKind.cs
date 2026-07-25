namespace ClaudyGod.Domain.Enums;

/// <summary>
/// What kind of file an upload session is for — drives which MIME/extension/size
/// policy applies and which S3 key prefix the object lands under.
/// </summary>
public enum UploadAssetKind
{
    Thumbnail,
    Audio,
    Video,
    Document
}
