using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLaun.Validators
{
    class PathValidator : ValidatorBase
    {
        public override string this[string columnName]
        {
            get
            {
                string error = null;
                if (columnName == nameof(Value))
                {
                    if (string.IsNullOrEmpty(Value))
                        error = "Путь не может быть пустым.";

                    if (!string.IsNullOrEmpty(Value) && !Directory.Exists(Value))
                        error = "Указанный путь не существует.";
                }
                return error;
            }
        }
    }
}
