using DigitalTrainingAssistant.Models;
using Newtonsoft.Json;
using System;

namespace DigitalTrainingAssistant.Bot.Dialogues
{
    public class AdaptiveCardUtils
    {
        public static ActionResponse GetAdaptiveCardAction(string submitJson, string fromAadObjectId)
        {
            ActionResponse r = null;

            try
            {
                r = JsonConvert.DeserializeObject<ActionResponse>(submitJson);
            }
            catch (Exception)
            {
                // Nothing
            }

            if (r != null)
            {
                if (r.Action == CardConstants.CardActionValLearnerTasksDone)
                {
                    var update = new CourseTasksUpdateInfo(submitJson, fromAadObjectId);

                    return update;
                }
                else if (r.Action == CardConstants.CardActionValStartIntroduction)
                {
                    var spAction = JsonConvert.DeserializeObject<ActionResponseForSharePointItem>(submitJson);
                    return spAction;
                }
                else if (r.Action == CardConstants.CardActionValSaveIntroductionQuestions)
                {
                    var introductionData = JsonConvert.DeserializeObject<IntroduceYourselfResponse>(submitJson);
                    return introductionData;
                }
            }
            


            return null;
        }
    }
}
