using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;

namespace Lab_3_Form_Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     This is the default handler for any message recieved from the user, we will start by using QnAMaker
        /// </summary>
        /// <param name="context">The current chat context</param>
        /// <param name="result">The IAwaitable result</param>
        /// <returns></returns>
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            try
            {
                // if the activity has a Value populated then it is the json from our adaptive card
                if (activity.Value != null)
                {
                    // parse the value field from our activity, this will be populated with the "data" field from our 
                    // adaptive card json if the user clicked the card's button, it will be an empty {{}}
                    // if our json did not define a "data" field
                    JToken valueToken = JObject.Parse(activity.Value.ToString());
                    string actionValue = valueToken.SelectToken("action") != null ? valueToken.SelectToken("action").ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(actionValue))
                    {
                        switch (valueToken.SelectToken("action").ToString())
                        {
                            case "jokes":
                                context.Call(new JokesDialog(), AfterJoke);
                                break;
                            case "trivia":
                                context.Call(new TriviaDialog(), AfterTrivia);
                                break;
                            default:
                                await context.PostAsync($"I don't know how to handle the action \"{actionValue}\"");
                                context.Wait(MessageReceivedAsync);
                                break;

                        }
                    }
                    else
                    {
                        await context.PostAsync("It looks like no \"data\" was defined for this. Check your adaptive cards json definition.");
                        context.Wait(MessageReceivedAsync);
                    }
                }
                else
                {
                    await context.Forward(new QnaDialog(), AfterQnA, activity, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                // if an error occured with QnAMaker post it out to the user
                await context.PostAsync(e.Message);
                // wait for the next message
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task AfterJoke(IDialogContext context, IAwaitable<string> result)
        {
            context.Wait(MessageReceivedAsync);
        }

        /// <summary>
        /// Ask if the user wants to take a survey after the trivia game.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task AfterTrivia(IDialogContext context, IAwaitable<string> result)
        {
            await context.PostAsync("Thanks for playing!");
            PromptDialog.Confirm(context, AfterAskingAboutSurvey, "Would you like to take a survey?");
        }
        
        private async Task AfterAskingAboutSurvey(IDialogContext context, IAwaitable<bool> result)
        {
            bool takeSurvey = await result;
            if (!takeSurvey)
            {
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                var survey = new FormDialog<SurveyForm>(new SurveyForm(), SurveyForm.BuildForm, FormOptions.PromptInStart, null);
                context.Call<SurveyForm>(survey, AfterSurvey);
            }
        }

        private async Task AfterSurvey(IDialogContext context, IAwaitable<SurveyForm> result)
        {

            SurveyForm survey = await result;
            await context.PostAsync("Thanks for taking the survey!");
            context.Wait(MessageReceivedAsync);
        }

        /// <summary>
        ///     This will get called after returning from the QnA
        /// </summary>
        /// <param name="context">The current chat context</param>
        /// <param name="result">The IAwaitable result</param>
        /// <returns></returns>
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

            if (((IMessageActivity)context.Activity).Text.ToLowerInvariant().Contains("trivia"))
            {
                // Since we are not needing to pass any message to start trivia, we can use call instead of forward
                context.Call(new TriviaDialog(), AfterTrivia);
            }
            else if (!string.IsNullOrEmpty(answer))
            {
                Activity reply = ((Activity)context.Activity).CreateReply();

                string[] qnaAnswerData = answer.Split('|');
                string title = qnaAnswerData[0];
                string description = qnaAnswerData[1];
                string url = qnaAnswerData[2];
                string imageURL = qnaAnswerData[3];

                if (title == "")
                {
                    char charsToTrim = '|';
                    await context.PostAsync(answer.Trim(charsToTrim));
                }

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
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                await context.PostAsync("No good match found in KB.");
                int length = (((IMessageActivity)context.Activity).Text ?? string.Empty).Length;
                await context.PostAsync($"You sent {((IMessageActivity)context.Activity).Text} which was {length} characters");
                context.Wait(MessageReceivedAsync);
            }
        }
    }
}