using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace appMpower; 

public class MultipleUpdatesMiddelware
{
    private readonly static KeyValuePair<string, StringValues> _headerServer =
         new KeyValuePair<string, StringValues>("Server", new StringValues("k"));
    private readonly static KeyValuePair<string, StringValues> _headerContentType =
         new KeyValuePair<string, StringValues>("Content-Type", new StringValues("application/json"));

   private readonly RequestDelegate _next;

   public MultipleUpdatesMiddelware(RequestDelegate next)
   {
      _next = next;
   }

    public unsafe Task Invoke(HttpContext httpContext)
   {
      if (httpContext.Request.Path.StartsWithSegments("/updates", StringComparison.Ordinal))
      {
         var queryString = httpContext.Request.QueryString.ToString(); 
         int count; 
         Int32.TryParse(queryString.Substring(queryString.LastIndexOf("=") + 1), out count); 
         count = count > 500 ? 500 : (count > 0 ? count : 1);

         var response = httpContext.Response;
         response.Headers.Add(_headerServer);
         response.Headers.Add(_headerContentType);

         int payloadLength;
         IntPtr handlePointer;

         IntPtr bytePointer = NativeMethods.Updates(count, out payloadLength, out handlePointer);
         byte[] json = new byte[payloadLength];
         Marshal.Copy(bytePointer, json, 0, payloadLength);
         NativeMethods.FreeHandlePointer(handlePointer);

         response.Headers.Add(
             new KeyValuePair<string, StringValues>("Content-Length", payloadLength.ToString()));

         return response.Body.WriteAsync(json, 0, payloadLength);
      }

      return _next(httpContext);
   }
}

public static class MultipleUpdatesMiddelwareExtensions
{
   public static IApplicationBuilder UseMultipleUpdates(this IApplicationBuilder builder)
   {
      return builder.UseMiddleware<MultipleUpdatesMiddelware>();
   }
}