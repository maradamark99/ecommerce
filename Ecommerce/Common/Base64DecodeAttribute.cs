using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Common;


[AttributeUsage(AttributeTargets.Parameter)]
public class Base64DecodeAttribute() : ModelBinderAttribute(typeof(Base64DecodeModelBinder));