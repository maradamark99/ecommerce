namespace Ecommerce.Common.Exception;

using System;   

[Serializable]
public class NotFoundException : Exception
{
    
    public NotFoundException()
    {
    }   
    
    public NotFoundException(string message) : base(message)
    {
    }   
    
}