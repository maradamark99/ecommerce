using EcommerceLib.File;

namespace ProductManagement.Contract;

public record ProductMediaResponse(long Id, string Url, FileType FileType, bool IsPrimaryImage);