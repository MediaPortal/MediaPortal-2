using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPExtended.Services.Common.Interfaces
{
    public class WebStringResult
    {
        public string Result { get; set; }

        public WebStringResult()
        {
        }

        public WebStringResult(string value)
        {
            Result = value;
        }

        public override string ToString()
        {
            return Result;
        }

        public override bool Equals(object obj)
        {
            WebStringResult r = obj is string ? new WebStringResult((string)obj) : obj as WebStringResult;
            return (object)r != null && this.Result == r.Result;
        }

        public override int GetHashCode()
        {
            return Result.GetHashCode();
        }

        public static bool operator ==(WebStringResult a, WebStringResult b)
        {
            return Object.ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Result == b.Result);
        }

        public static bool operator !=(WebStringResult a, WebStringResult b)
        {
            return !(a == b);
        }

        public static implicit operator WebStringResult(string value)
        {
            return new WebStringResult(value);
        }

        public static implicit operator string(WebStringResult value)
        {
            return value.Result;
        }
    }
}
