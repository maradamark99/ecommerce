using Ecommerce.Common.File;

namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public record ProductMediaResponse(long Id, string Url, FileType FileType, bool IsPrimaryImage);