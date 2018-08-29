using System;
using Microsoft.Bot.Builder.FormFlow;

namespace Lab_3_1_LUIS_Bot
{
    public enum DepartmentOptions
    {
        Accounting,
        AdministrativeSupport,
        IT
    }

    [Serializable]
    public class SurveyDialog
    {
        [Prompt("Please enter your {&}.")]
        public string Name;

        [Prompt("Please enter your {&}.")]
        [Pattern(@"(<Undefined control sequence>\d)?\s*\d{3}(-|\s*)\d{4}")]
        public string PhoneNumber;

        [Prompt("Please enter your {&}.")]
        [Pattern(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
        public string EmailAddress;

        [Prompt("What {&} do you work in? {||}.")]
        public DepartmentOptions? Department;

        public static IForm<SurveyDialog> BuildForm()
        {
            return new FormBuilder<SurveyDialog>().Build();
        }
    }
}