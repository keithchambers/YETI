//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace YammerLibrary
{
    using System;
    using System.Collections.Generic;

    public class Yammer_GetThrdsAndUsrsToGenFls_Result
    {
        public List<Yammer_GetThrdsToGenFls_Result> ThrdsResult { get; set; }
        public List<Yammer_GetUsrsToGenFls_Result> UsrsResult { get; set; }

    }
    public partial class Yammer_GetThrdsToGenFls_Result
    {
        public Nullable<int> MessageCount { get; set; }
        public Nullable<long> thread_id { get; set; }
    }

    public partial class Yammer_GetUsrsToGenFls_Result
    {
        public Nullable<int> UserId { get; set; }
        public string FullName { get; set; }
        public string EmailAlias { get; set; }
    }
}