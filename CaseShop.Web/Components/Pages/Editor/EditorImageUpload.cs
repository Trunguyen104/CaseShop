namespace CaseShop.Web.Components.Pages.Editor;

public sealed record EditorImageUpload(
    byte[] Bytes,
    string FileName,
    string ContentType);
