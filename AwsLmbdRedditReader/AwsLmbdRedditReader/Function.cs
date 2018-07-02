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

            RedditAccess reddit = new RedditAccess(log);
            SkillResponse response = null;


            log.LogLine("FunctionHandler called");

            var requestType = input.GetRequestType();

            if (requestType == typeof(LaunchRequest))
            {
                if (input.Context.System.User.AccessToken == null)
                {
                    LinkAccountCard lc = new LinkAccountCard();
                    response = MakeSkillResponseWithLinkAccountCard($"Browse Reddit with your voice. Ask for news or tell me what subreddit you want to browse.", false, lc, "Use the help command if you want to know about all the navigation features.");
                }
                else
                {
                    response = MakeSkillResponse($"Browse Reddit with your voice. Ask for news or tell me what subreddit you want to browse.", false, "Use the help command if you want to know about all the navigation features.");
                }
            }
            else if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;


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

                    case "AboutPost":
                        Tuple<SkillResponse, CurrentSession> tAboutPost = reddit.aboutPost(cs);
                        response = tAboutPost.Item1;
                        cs = tAboutPost.Item2;
                        break;
                    case "RepeatPost":
                        response = reddit.repeatPost(cs);
                        break;
                    case "randomPost":
                        Tuple<SkillResponse, CurrentSession> tRandom = reddit.randomPost();
                        response = tRandom.Item1;
                        cs = tRandom.Item2;
                        break;
                    case "AMAZON.StopIntent":
                        //stopping skill
                        response = MakeSkillResponse($"", true);
                        break;
                    case "AMAZON.CancelIntent":
                        //stopping skill
                        response = MakeSkillResponse($"", true);
                        break;
                    case "AMAZON.HelpIntent":
                        response = MakeSkillResponse($"If you want to get a news update say: Tell me the news. " +
                            $"If you want to browse a Subreddit, say: " +
                            $"Browse and the name of the subreddit. If you want a surprise, say: Tell me a random post.", false);
                        break;
                }
            }
            if (response == null)
            {
                String errorreprompt = "repeat yourself please.";
                response = MakeSkillResponse($"Sorry. I didnt understand that, {errorreprompt}", false, errorreprompt);
            }
            if(cs != null)
            {
                response.SessionAttributes = cs.storeSession();
            }

            return response;
        }

        public static SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "Reprompt Text")
        //change default reprompt text
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
        //change default reprompt text
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

        public static SkillResponse MakeSkillResponseWithLinkAccountCard(string outputSpeech,
    bool shouldEndSession, LinkAccountCard card,
    string repromptText = "Reprompt Text")
        //change default reprompt text
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
