using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FLaun.Validators
{
    class UsernameValidator : ValidatorBase
    {
        public override string this[string columnName]
        {
            get
            {
                string error = null;
                if (columnName == nameof(Value))
                {
                    if (!string.IsNullOrEmpty(Value) && !Regex.IsMatch(Value, @"^[A-Za-z0-9~!@#$%^&*():?><.,/|\'+=_-]+$"))
                        error = "Никнейм содержит недопустимые символы.";

                    if (string.IsNullOrEmpty(Value))
                        error = "Пожалуйста, заполните поле или выберите другой тип профиля.";
                }
                return error;
            }
        }
    }
}
