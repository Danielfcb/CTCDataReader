using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTCDataReader.Utilties
{
    interface iDataValidator<dataModel>
    {
        List<dataModel> ValidRecords { get; }
        List<string> InvalidRecords { get; }
        List<dataModel> DuplicatedRecords { get; }
    }
}
