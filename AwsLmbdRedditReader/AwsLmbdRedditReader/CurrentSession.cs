using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;

namespace AwsLmbdRedditReader
{
    class CurrentSession
    {
        ILambdaLogger log;

        public String subreddit { get; set; }
        public int postNumber { get; set; }
        public String selfText { get; set; }
        public String title { get; set; }
        public bool inTitleMode { get; set; }

        public CurrentSession(ILambdaLogger log)
        {
            this.log = log;
        }

        public CurrentSession(ILambdaLogger log, String subreddit, int postNumber, String selfText, String title, bool inTitleMode)
        {
            this.log = log;
            this.subreddit = subreddit;
            this.postNumber = postNumber;
            this.selfText = selfText;
            this.title = title;
            this.inTitleMode = inTitleMode;
        }

        public Dictionary<string, object> storeSession()
        {
            Dictionary<string, object> sessionAttributes = new Dictionary<string, object>();
            sessionAttributes.Add("currentSubreddit", subreddit);
            sessionAttributes.Add("currentPostNumber", postNumber + "");
            sessionAttributes.Add("currentPostSelfText", selfText);
            sessionAttributes.Add("currentPostTitle", title);
            sessionAttributes.Add("inTitleMode", inTitleMode);
            log.LogLine($"StoreSession CurrentSession = {this}");

            return sessionAttributes;

        }

        public static CurrentSession retrieveCurrentSessionFromSessionAttributes(ILambdaLogger log, Dictionary<String, object> sessionAttributes)
        {
            CurrentSession cs = new CurrentSession(log);
            cs.subreddit = (String)sessionAttributes["currentSubreddit"];
            cs.postNumber = int.Parse((String)sessionAttributes["currentPostNumber"]);
            cs.selfText = (String)sessionAttributes["currentPostSelfText"];
            cs.title = (String)sessionAttributes["currentPostTitle"];
            cs.inTitleMode = (bool)sessionAttributes["inTitleMode"];
            log.LogLine($"RetrieveSessionFromSessionAttributes CurrentSession = {cs}");

            return cs;
        }



        public override string ToString()
        {
            return $"Subreddit: {subreddit}, PostNumber = {postNumber}, Title = { title}, SelfText = {selfText}, inTitleMode= {inTitleMode}"; ;
        }

    }
}


