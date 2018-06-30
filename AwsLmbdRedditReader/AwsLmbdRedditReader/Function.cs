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
        String currentSubreddit = "";
        String currentPostSelfText = "";
        int currentPostNumber = 1;

        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {

           log = context.Logger;
            Dictionary<String, object> sessionAttributes = input.Session.Attributes;
            log.LogLine($"SessionAttributesContent: {sessionAttributes}");

            if (sessionAttributes !=null )
            {
                currentSubreddit = (String) sessionAttributes["currentSubreddit"];
                currentPostNumber = int.Parse((String) sessionAttributes["currentPostNumber"]);
                currentPostSelfText = (String) sessionAttributes["currentPostSelfText"];
                log.LogLine($"Input SessionAttributes = Subreddit: '{currentSubreddit}', Postnumber: '{currentPostNumber}'");
            }


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
                String standardRepromptText = "Details, Next or Back?";

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
                        Post firstPost = postTask.Result;
                        log.LogLine($"Post retrieved: {firstPost}");

                        //Store Session
                        currentSubreddit = subredditSlot;
                        currentPostNumber = 1;
                        currentPostSelfText = firstPost.SelfText;

                        response = MakeSkillResponse($"{subredditSlot} is now selected. " +
                            $"First post: {firstPost.Title}. To navigate say details, next, back or repeat.", false, standardRepromptText);
                       

                        response.SessionAttributes = new Dictionary<string, object>();
                        response.SessionAttributes.Add("currentSubreddit", currentSubreddit);
                        response.SessionAttributes.Add("currentPostNumber", currentPostNumber + "");
                        response.SessionAttributes.Add("currentPostSelfText", currentPostSelfText);
                        log.LogLine($"SessionAttributes IN ChooseSubreddit: \n {response.SessionAttributes}");

                        break;
                    case "NextPost":
                        if (currentSubreddit.Equals(""))
                        {
                            String repromptText= "Please tell me which subreddit you want to browse.";
                            response = MakeSkillResponse($"No subreddit selected. {repromptText}", false, repromptText);
                        }
                        else
                        {
                            var contSubredditTask = reddit.SearchSubreddits(currentSubreddit).First();
                            contSubredditTask.Wait();
                            Subreddit contSubreddit = contSubredditTask.Result;
                            log.LogLine($"ContSubreddit Selected: {contSubreddit}");

                            currentPostNumber++;

                            Task<Post> contPostTask = contSubreddit.GetPosts(currentPostNumber).Last();
                            Post contPost = contPostTask.Result;
                            log.LogLine($"Post retrieved: {contPost}");
                            currentPostSelfText = contPost.SelfText;

                            response = MakeSkillResponse($"Next Post. {contPost.Title}.", false, standardRepromptText);

                            //Store Session


                            response.SessionAttributes = new Dictionary<string, object>();
                            response.SessionAttributes.Add("currentSubreddit", currentSubreddit);
                            response.SessionAttributes.Add("currentPostNumber", currentPostNumber + "");
                            response.SessionAttributes.Add("currentPostSelfText", currentPostSelfText);
                            log.LogLine($"SessionAttributes IN NextPost: \n {response.SessionAttributes}");
                        }

                        break;

                    case "PreviousPost":
                        if (currentSubreddit.Equals(""))
                        {
                            String repromptText = "Please tell me which subreddit you want to browse.";
                            response = MakeSkillResponse($"No subreddit selected. {repromptText}", false, repromptText);
                        }
                        else
                        {
                            var backSubredditTask = reddit.SearchSubreddits(currentSubreddit).First();
                            backSubredditTask.Wait();
                            Subreddit backSubreddit = backSubredditTask.Result;
                            log.LogLine($"BackSubreddit Selected: {backSubreddit}");

                            String backIntro="";
                            if(currentPostNumber > 1)
                            {
                                currentPostNumber--;
                                backIntro = "Previous Post.";
                            }
                            else
                            {
                                currentPostNumber = 1;
                                backIntro = "Can't go back any further.";
                            }
                            

                            Task<Post> backPostTask = backSubreddit.GetPosts(currentPostNumber).Last();
                            Post backPost = backPostTask.Result;
                            log.LogLine($"Post retrieved: {backPost}");
                            currentPostSelfText = backPost.SelfText;

                            response = MakeSkillResponse($"{backIntro} {backPost.Title}.", false, standardRepromptText);

                            //Store Session


                            response.SessionAttributes = new Dictionary<string, object>();
                            response.SessionAttributes.Add("currentSubreddit", currentSubreddit);
                            response.SessionAttributes.Add("currentPostNumber", currentPostNumber + "");
                            response.SessionAttributes.Add("currentPostSelfText", currentPostSelfText);
                            log.LogLine($"SessionAttributes IN PreviousPost: \n {response.SessionAttributes}");
                        }

                        break;
                    case "AboutPost":
                        if (currentSubreddit.Equals(""))
                        {
                            String repromptText = "Please tell me which subreddit you want to browse.";
                            response = MakeSkillResponse($"No subreddit selected. {repromptText}", false, repromptText);
                        }
                        else
                        {
                            response = MakeSkillResponse($"{currentPostSelfText}.", false, standardRepromptText);

                            //Store Session
                            response.SessionAttributes = new Dictionary<string, object>();
                            response.SessionAttributes.Add("currentSubreddit", currentSubreddit);
                            response.SessionAttributes.Add("currentPostNumber", currentPostNumber + "");
                            response.SessionAttributes.Add("currentPostSelfText", currentPostSelfText);
                            log.LogLine($"SessionAttributes IN NextPost: \n {response.SessionAttributes}");
                        }

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
                String errorreprompt = "repeat yourself please.";
                response = MakeSkillResponse($"Sorry. I didnt understand that, {errorreprompt}", false, errorreprompt);
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
