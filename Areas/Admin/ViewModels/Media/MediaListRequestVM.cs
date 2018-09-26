﻿using Bonsai.Areas.Admin.ViewModels.Common;
using Bonsai.Data.Models;

namespace Bonsai.Areas.Admin.ViewModels.Media
{
    /// <summary>
    /// The request for filtering media files.
    /// </summary>
    public class MediaListRequestVM: ListRequestVM
    {
        /// <summary>
        /// Related media types.
        /// </summary>
        public MediaType[] Types { get; set; }
        
        /// <summary>
        /// Checks if the request has no filter applied.
        /// </summary>
        public override bool IsEmpty()
        {
            return base.IsEmpty()
                   && (Types == null || Types.Length == 0);
        }
    }
}
