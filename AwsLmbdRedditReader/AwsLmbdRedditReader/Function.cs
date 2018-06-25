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
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {

           log = context.Logger;

            SkillResponse response = null;
            
           Reddit reddit = new Reddit();
            //Subreddit sr = reddit.RSlashAll;
            //var getPostsTask = sr.GetPosts(1).First() ;
            //getPostsTask.Wait();
            //Post p = getPostsTask.Result;
            // //subrredittask.Wait();

            
            log.LogLine("FunctionHandler called");

            var requestType = input.GetRequestType();

            if (requestType == typeof(LaunchRequest))
            {
                response = MakeSkillResponse($"Browse Reddit with your voice. Ask for news or tell me what subreddit you want to browse.", false);
            }
            else if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;

                switch (intentRequest.Intent.Name)
                {
                    case "FlashBriefing":
                        var subredditTask = reddit.SearchSubreddits("worldnews", 1).First();
                        subredditTask.Wait();
                        var sr = subredditTask.Result;
                        log.LogLine($"WorldNews Subreddit: {sr}");

                        //var sr = reddit.RSlashAll;
                        var posts = sr.GetPosts(3).ToList<Post>();
                        posts.Wait();
                       
                        
                        log.LogLine("Posts (toString): " + posts.ToString());
                        var i = 0;
                        String[] postIntro = { "First Post", "Second Post", "Third Post" };
                        String responseString = "These are your top three Posts on Reddit. ";
                        List<Post> storedPosts = posts.Result;
                        String imageURL = "";
                        foreach (var post in storedPosts)
                        {     
                            log.LogLine($"Post Title ({i}) | {postIntro[i]}: {post.Title}");
                            responseString += $"{postIntro[i]}: {post.Title}. ";
                            i++;

                            String img = storedPosts[0].Thumbnail.ToString();
                            if (img != null && img != "")
                            {
                                imageURL = img;
                                break;
                            }
                        }
                        responseString += "See the feed in your Alexa App for more Info.";
                        log.LogLine("FullSkillResponse FlashBriefing: " + responseString);

                        //CARD
                        StandardCard card = new StandardCard();
                        card.Title = "Your Top 3 Posts from Reddit";

                             String cardContent = $"{storedPosts[0].Title} (See more: {storedPosts[0].Url})\n\n" +
                            $"{storedPosts[1].Title} (See more: {storedPosts[1].Url})\n\n" +
                            $"{storedPosts[2].Title} (See more: {storedPosts[2].Url})";
                        card.Content = cardContent;
                        log.LogLine("FlashBriefingCard CardContent: " + cardContent);

                        //IMAGE FOR CARD
                        if(imageURL != "") {
                            CardImage cardImage = new CardImage();
                            log.LogLine("FlashBriefingCard CardImageURL: " + imageURL);
                            cardImage.SmallImageUrl = imageURL;
                            cardImage.LargeImageUrl = imageURL;
                            card.Image = cardImage;
                        }
                        else
                        {
                            log.LogLine("FlashBriefingCard CardImageURL: No post had an image");
                        }

                        response = MakeSkillResponseWithCard(responseString, false, card);

                        break;
                    case "ChooseSubreddit":
                        var subredditSlot = intentRequest.Intent.Slots["SubReddit"].Value;

                        var chooseSubredditTask = reddit.SearchSubreddits(subredditSlot, 1).First();
                        chooseSubredditTask.Wait();
                        var chosenSR = chooseSubredditTask.Result;
                        log.LogLine($"Chosen Subreddit: {chosenSR}");

                        var postTask = chosenSR.GetPosts(1).First();
                        var firstPost = postTask.Result;

                        response = MakeSkillResponse($"{subredditSlot} is now selected. First post: {firstPost.Title}. About, Continue or Back?", false);
                        break;
                    case "StepForward":
                        String nextTitle = "Thunderstorm in Ohio";
                        response = MakeSkillResponse($"{nextTitle}", false);
                        //logic
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
                        response = MakeSkillResponse($"Help is here", true);
                        break;
                }                 
            }
            if (response == null)
            {
                String errorreprompt = "Führe eine Aktion aus oder frage nach Hilfe";
                response = MakeSkillResponse($"Anfrage wurde nicht verstanden. {errorreprompt}", false, errorreprompt);
            }
            return response;
        }

        private SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "Reprompt Text" )
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

        private SkillResponse MakeSkillResponseWithCard(string outputSpeech,
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

    }
}
