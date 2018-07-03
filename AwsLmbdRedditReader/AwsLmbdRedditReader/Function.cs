using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using RedditSharp;
using RedditSharp.Things;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AwsLmbdRedditReader
{
    public class Function
    {
        //main class. entry point when lambda function is called. handles the different incoming intents
        ILambdaLogger log;
        CurrentSession cs;

        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {

            log = context.Logger;
            Dictionary<String, object> sessionAttributes = input.Session.Attributes;
            log.LogLine($"SessionAttributesContent: {sessionAttributes}");

            
            if (sessionAttributes != null)
            {
                cs = CurrentSession.retrieveCurrentSessionFromSessionAttributes(log, sessionAttributes);
            }

            //checks if the client supports display output 
            bool supportsDisplay = input.Context.System.Device.SupportedInterfaces.ContainsKey("Display");

            RedditAccess reddit = new RedditAccess(log, supportsDisplay);
            SkillResponse response = null;

            log.LogLine("FunctionHandler called");


            var requestType = input.GetRequestType();
            if (requestType == typeof(LaunchRequest))
            {
                //launchrequest is called when the user starts the skill - "alexa, start reddit reader"
                String launchRepromptText = "Ask for news or tell me what subreddit you want to browse.";
                String launchText = $"Browse Reddit with your voice. {launchRepromptText}";

                if (input.Context.System.User.AccessToken == null)
                {
                    LinkAccountCard lc = new LinkAccountCard();
                    response = MakeSkillResponseWithLinkAccountCard(launchText, false, lc, launchRepromptText);
                }
                else
                {
                    response = MakeSkillResponse(launchText, false, launchRepromptText);
                }
            }
            else if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                //IntenRequests are defined in the Amazon Developer Console and are used to handle custom input and behaviour

                switch (intentRequest.Intent.Name)
                {
                    case "FlashBriefing":
                        response = reddit.flashBriefing();

                        break;
                    case "ChooseSubreddit":
                        String subredditSlot = intentRequest.Intent.Slots["SubReddit"].Value;
                        Tuple<SkillResponse, CurrentSession> tChooseSubreddit = reddit.chooseSubreddit(subredditSlot);
                        response = tChooseSubreddit.Item1;
                        cs = tChooseSubreddit.Item2;
                        break;

                    case "NextPost":
                        Tuple<SkillResponse, CurrentSession> tNextPost = reddit.nextPost(cs);
                        response = tNextPost.Item1;
                        cs = tNextPost.Item2;
                        break;

                    case "PreviousPost":
                        Tuple<SkillResponse, CurrentSession> tPreviousPost = reddit.previousPost(cs);
                        response = tPreviousPost.Item1;
                        cs = tPreviousPost.Item2;
                        break;
                    case "RandomPost":
                        Tuple<SkillResponse, CurrentSession> tRandom = reddit.randomPost();
                        response = tRandom.Item1;
                        cs = tRandom.Item2;
                        break;
                    case "AboutPost":
                        Tuple<SkillResponse, CurrentSession> tAboutPost = reddit.aboutPost(cs);
                        response = tAboutPost.Item1;
                        cs = tAboutPost.Item2;
                        break;
                    case "RepeatPost":
                        response = reddit.repeatPost(cs);
                        break;
                    case "AMAZON.StopIntent":
                        response = MakeSkillResponse($"", true);
                        break;
                    case "AMAZON.CancelIntent":
                        response = MakeSkillResponse($"", true);
                        break;
                    case "AMAZON.HelpIntent":
                        response = MakeSkillResponse($"If you want to get a news update say: Tell me the news. " +
                            $"If you want to browse a Subreddit, say: " +
                            $"Browse and the name of the subreddit.", false);
                        break;
                }
            }
            if (response == null)
            {
                String errorreprompt = "repeat yourself please.";
                response = MakeSkillResponse($"Sorry. I didnt understand that, {errorreprompt}", false, errorreprompt);
            }
            if (cs != null)
            {
                response.SessionAttributes = cs.storeSession();
            }

            return response;
        }

        public static SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "Reprompt Text")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            return skillResponse;
        }

        public static SkillResponse MakeSkillResponseWithCard(string outputSpeech,
            bool shouldEndSession, StandardCard card,
            string repromptText = "Reprompt Text")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            if (card != null)
            {
                response.Card = card;
            }
            return skillResponse;
        }

        public static SkillResponse MakeSkillResponseWithDirectives(string outputSpeech,
    bool shouldEndSession, List<IDirective> directives,
    string repromptText = "Reprompt Text")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            if (directives != null)
            {
                response.Directives = directives;
            }

            return skillResponse;
        }

        public static SkillResponse MakeSkillResponseWithLinkAccountCard(string outputSpeech,
    bool shouldEndSession, LinkAccountCard card,
    string repromptText = "Reprompt Text")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            if (card != null)
            {
                response.Card = card;
            }
            return skillResponse;
        }

    }
}
