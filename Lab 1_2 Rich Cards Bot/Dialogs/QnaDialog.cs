using System;
using System.Configuration;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Web;

namespace Lab_1_2_Rich_Cards_Bot.Dialogs
{
    [Serializable]
    public class QnaDialog : IDialog<object>
    {
        private QnAMakerService _QnAService = new QnAMakerService(
                ConfigurationManager.AppSettings["QnAHost"],
                ConfigurationManager.AppSettings["QnAKnowledgebaseId"],
                ConfigurationManager.AppSettings["QnAEndPointKey"]);
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedASync);
            return Task.CompletedTask;
        }

        public async Task MessageReceivedASync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            string safeText = HttpUtility.UrlEncode(((IMessageActivity)context.Activity).Text);
            string answer = _QnAService.GetAnswer(safeText);
            context.Done(answer);
        }
    }
}