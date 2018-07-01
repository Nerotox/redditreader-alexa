using RedditSharp;
using System;
using System.Collections.Generic;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using System.Linq;
using RedditSharp.Things;
using System.Threading.Tasks;

namespace AwsLmbdRedditReader
{
    class RedditAccess
    {
        Reddit reddit;
        ILambdaLogger log;
        const String SUBREDDIT_FOR_FLASHBRIEFING = "worldnews";
        const String BROWSING_REPROMPT_TEXT = "Details, Next or Back?";
        const String NO_SUBREDDIT_SELECTED = "No subreddit selected.";
        const String NO_SUBREDDIT_SELECTED_REPROMPT = "Please tell me which subreddit you want to browse.";

        public RedditAccess(ILambdaLogger log)
        {
            reddit = new Reddit();
            this.log = log;
        }


        public SkillResponse flashBriefing()
        {
            String responseString = "These are the top three news posts on Reddit. ";

            var subredditTask = reddit.SearchSubreddits(SUBREDDIT_FOR_FLASHBRIEFING, 1).First();
            subredditTask.Wait();
            var sr = subredditTask.Result;
            log.LogLine($"WorldNews Subreddit: {sr}");

            var posts = sr.GetPosts(3).ToList<Post>();
            posts.Wait();


            log.LogLine("Posts (toString): " + posts.ToString());
            var i = 0;
            String[] postIntro = { "First Post", "Second Post", "Third Post" };

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
            card.Title = "The Top 3 News from Reddit";

            String cardContent = $"{storedPosts[0].Title} (See more: {storedPosts[0].Url})\n\n" +
           $"{storedPosts[1].Title} (See more: {storedPosts[1].Url})\n\n" +
           $"{storedPosts[2].Title} (See more: {storedPosts[2].Url})";
            card.Content = cardContent;
            log.LogLine("FlashBriefingCard CardContent: " + cardContent);

            //IMAGE FOR CARD
            if (imageURL != "")
            {
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

            return Function.MakeSkillResponseWithCard(responseString, true, card);
        }

        internal Tuple<SkillResponse, CurrentSession> aboutPost(CurrentSession cs)
        {
            SkillResponse response;
            if (cs == null)
            {
                response = Function.MakeSkillResponse($"{NO_SUBREDDIT_SELECTED} {NO_SUBREDDIT_SELECTED_REPROMPT}", false, NO_SUBREDDIT_SELECTED_REPROMPT);
            }
            else
            {
                response = Function.MakeSkillResponse($"{cs.selfText}.", false, BROWSING_REPROMPT_TEXT);
                cs.inTitleMode = false;
            }
            return new Tuple<SkillResponse, CurrentSession>(response, cs);
        }

        internal SkillResponse repeatPost(CurrentSession cs)
        {
            SkillResponse response;
            if (cs == null)
            {
                response = Function.MakeSkillResponse($"{NO_SUBREDDIT_SELECTED} {NO_SUBREDDIT_SELECTED_REPROMPT}", false, NO_SUBREDDIT_SELECTED_REPROMPT);
            }
            else
            {
                if (cs.inTitleMode)
                {
                    response = Function.MakeSkillResponse($"Repeating Post: { cs.title}", false, BROWSING_REPROMPT_TEXT);
                }
                else
                {
                    response = Function.MakeSkillResponse($"Repeating Details From Post: { cs.selfText}", false, BROWSING_REPROMPT_TEXT);
                }
            }
            return response;
        }

        internal Tuple<SkillResponse, CurrentSession> previousPost(CurrentSession cs)
        {
            SkillResponse response;
            if (cs == null)
            {
                response = Function.MakeSkillResponse($"{NO_SUBREDDIT_SELECTED} {NO_SUBREDDIT_SELECTED_REPROMPT}", false, NO_SUBREDDIT_SELECTED_REPROMPT);
            }
            else
            {
                var backSubredditTask = reddit.SearchSubreddits(cs.subreddit).First();
                backSubredditTask.Wait();
                Subreddit backSubreddit = backSubredditTask.Result;
                log.LogLine($"BackSubreddit Selected: {backSubreddit}");

                String backIntro = "";
                if (cs.postNumber > 1)
                {
                    cs.postNumber--;
                    backIntro = "Previous Post.";
                }
                else
                {
                    cs.postNumber = 1;
                    backIntro = "Can't go back any further.";
                }

                Task<Post> backPostTask = backSubreddit.GetPosts(cs.postNumber).Last();
                Post backPost = backPostTask.Result;
                log.LogLine($"Post retrieved: {backPost}");
                cs.selfText = backPost.SelfText;
                cs.title = backPost.Title;
                cs.inTitleMode = true;

                response = Function.MakeSkillResponse($"{backIntro} {backPost.Title}.", false, BROWSING_REPROMPT_TEXT);
            }
            return new Tuple<SkillResponse, CurrentSession>(response, cs);
        }

        internal Tuple<SkillResponse, CurrentSession> nextPost(CurrentSession cs)
        {
            SkillResponse response;
            if (cs == null)
            {
                response = Function.MakeSkillResponse($"{NO_SUBREDDIT_SELECTED} {NO_SUBREDDIT_SELECTED_REPROMPT}", false, NO_SUBREDDIT_SELECTED_REPROMPT);
            }
            else
            {
                var contSubredditTask = reddit.SearchSubreddits(cs.subreddit).First();
                contSubredditTask.Wait();
                Subreddit contSubreddit = contSubredditTask.Result;
                log.LogLine($"ContSubreddit Selected: {contSubreddit}");

                cs.postNumber++;

                Task<Post> contPostTask = contSubreddit.GetPosts(cs.postNumber).Last();
                Post contPost = contPostTask.Result;
                log.LogLine($"Post retrieved: {contPost}");
                cs.selfText = contPost.SelfText;
                cs.title = contPost.Title;
                cs.inTitleMode = true;

                response = Function.MakeSkillResponse($"Next Post. {cs.title}.", false, BROWSING_REPROMPT_TEXT);
            }
            return new Tuple<SkillResponse, CurrentSession>(response, cs);

        }

        internal Tuple<SkillResponse, CurrentSession> chooseSubreddit(String subredditSlot)
        {

            var chooseSubredditTask = reddit.SearchSubreddits(subredditSlot, 1).First();
            chooseSubredditTask.Wait();
            var chosenSR = chooseSubredditTask.Result;
            log.LogLine($"Chosen Subreddit: {chosenSR}");

            var postTask = chosenSR.GetPosts(1).First();
            Post firstPost = postTask.Result;
            log.LogLine($"Post retrieved: {firstPost}");


            CurrentSession cs = new CurrentSession(log, subredditSlot, 1, firstPost.SelfText, firstPost.Title, true);

            SkillResponse response = Function.MakeSkillResponse($"{subredditSlot} is now selected. " +
                $"First post: {firstPost.Title}. To navigate say details, next, back or repeat.", false, BROWSING_REPROMPT_TEXT);

            return new Tuple<SkillResponse, CurrentSession>(response, cs);
        }
    }
}
