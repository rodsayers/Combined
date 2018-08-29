using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lab_1_3_Extra_Exercise.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            await context.Forward(new QnaDialog(), AfterQnA, activity, CancellationToken.None);
        }

        private async Task AfterQnA(IDialogContext context, IAwaitable<object> result)
        {
            string message = null;

            try
            {
                message = (string)await result;
            }
            catch (Exception e)
            {
                await context.PostAsync($"QnAMaker: {e.Message}");
                // Wait for the next message
                context.Wait(MessageReceivedAsync);
            }

            // Display the answer from QnA Maker Service
            var answer = message;

            if (!string.IsNullOrEmpty(answer))
            {
                Activity reply = ((Activity)context.Activity).CreateReply();

                string[] qnaAnswerData = answer.Split('|');
                string title = qnaAnswerData[0];
                string description = qnaAnswerData[1];
                string url = qnaAnswerData[2];
                string imageURL = qnaAnswerData[3];

                // !!!CBT - If no title supplied, then return simple answer
                if (title == "")
                {
                    char charsToTrim = '|';
                    await context.PostAsync(answer.Trim(charsToTrim));
                }
                // Otherwise return rich card
                else
                {
                    HeroCard card = new HeroCard
                    {
                        Title = title,
                        Subtitle = description,
                    };
                    card.Buttons = new List<CardAction>
            {
                new CardAction(ActionTypes.OpenUrl, "Learn More", value: url)
            };
                    card.Images = new List<CardImage>
            {
                new CardImage( url = imageURL)
            };
                    reply.Attachments.Add(card.ToAttachment());
                    await context.PostAsync(reply);
                }
            }
            else
            {
                await context.PostAsync("No good match found in KB.");
                int length = (((IMessageActivity)context.Activity).Text ?? string.Empty).Length;
                await context.PostAsync($"You sent {((IMessageActivity)context.Activity).Text} which was {length} characters");
            }
            context.Wait(MessageReceivedAsync);
        }
    }
}