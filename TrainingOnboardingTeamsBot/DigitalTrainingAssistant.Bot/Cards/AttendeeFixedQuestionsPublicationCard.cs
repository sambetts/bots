using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Graph;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Cards
{

    /// <summary>
    /// Class that helps to return introduction detail card as attachment.
    /// </summary>
    public class AttendeeFixedQuestionsPublicationCard : BaseAdaptiveCard
    {
        const string DEFAULT_IMG = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAABuwAAAbsBOuzj4gAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAUbSURBVHic5ZvBa1RHHMc/s2EloEmhmpKkbSKFmmColBQJiJemh9KaUBqv6UEUPAhe/At6iEdPHhSvvXmQYEnx0IaCIIgtiUVtDg3bdpUa6qGmQWUlvx7eLC5vf7P7dt689zb1C8PCvPd+v+/vOzNvZn5v1ogIecAYsxeYBT4B3gGGbQF4ZEsV+B64LiJPciEmIpkVoATMA8vAS0ASlpf2mXmglCnHDIM/BtztIGhXuQsc2zECAH3AYoDA42UR6OtqAYDRQK3eqjeMhuRsLPHUMMZMAD8Ab7W47Z5tyWXgT6IXH0Qvw3eBj4EvgIkWNjaAaRG5l5YzEKYHAAPAOu6WWwYOd2DvsH3GZW8dGOiKIQDsAm46iD4Fjqewfdza0GzfBHZ1gwDnHQR/AyYC2J+wtjQf5wsVABgCthRij4GREF3U+hmxNuN+toChIgW4pJB6DhwJFXyDryPWdtzfpUIEAAaBmkJoIXTwDT4XFH81YLAIAU4rZDaA/gwF6Lc+4n5P+9os4Y8vlbqLIvI0hc2WsLYvJuSSCF4LIWNMH/AEKMcuHRKRX3zJJPT9AdGKsBE1YK+IbHZqz7cHvE9z8JWsgwewPiqx6rLl1DF8BRhW6sIsTZNB86VxaouQAjxS6rKC5itXAd5U6v72tOUDzZfGqS18BfhLqdvnacsHmi+NU1v4CvC7UufVBT2h+dI4tYWvABWlrtUePjQ0XxUfQ74CVIHtWN1+O0dnCutjf6x623LqGF4CiEgNuK1c8l6RdQDNx23LqXOkWJefpHv2AieL2AztATYVMnnvBjeBPbkLYAldUQjlnQ+4kspuSlLjwDOFVF4ZoWfAeGECWHJnFWJ55QTPprYfgKABbjgIZpkVvoHdzhcqgCU65Hg718syYb8LbJAyGVovIb8MjQPf0bxIaUSIL0MV4DMR+TUdY4sQKja03CBwB3fLpS13SJEAzbQH1GGM2U301ebDoIZhBTgqIlshjaZJijbBGPMpsET44LE2l6yPcAjQ7Q0wR7ZdXxsKcxQ5C9jAvwLu5xh4vNy3HLyF8E2LvwF8A8wkfKRKNDTWgYdEb//6L0SzwNsNv+8BnxMdpkqCb4F5Efkn4f2v4NHyB4E12rfOCvA1MJmil01aGysJ/K0BBzMdAkTjTtsBNparwFjasan4HrO2W/neBOaCC0A0WywQZV5czn8EpkIHrnCZsr5cPLYt10TH65I47AGutXBYAWazDlzhNWt9u3hdA3pCCHChTasHOavjKcJAm95wIZUAwIkWxi8D5aKCb+BYtlxcPE94CQAcBV4oBmvAmaIDV/ieQT+w8YJoCZ1cAKIDj1oGRrox+JgIGufHOA5YugzdcnX7ooNMIIJrONxKJADRXO964RU+5hMIUG7xYmxaI8Qf7gEeKA9Winzbe4gwgD5FPiA2NcYfPOVQLvd5PoAIs45YTqkCAL1Em5amrl90MClE0IZCFejVBDjnUCzz5W2GAkw5YjqnCbCq3Hi16CACiKBtoFbr142IYIwZQT9gMC4ia0r9joExZgzQMsijIvJHPSeoJTZWd3rwADaGVeXSDLxKimoCLGZFqgBoscxAlNfbTXTqqjd2w0ci8nPGxHKBMWYS+ClW/RzYVwKmaQ6++n8JHsDGEj9C0wtMl4ADyjNLmbPKH1pMB0roR87WMyZTBLSYhktEX3bjeJgxmSKgxTTk6gF5nvvNC+r54te+BxjsKavYhX7x+PNBN8P+ySP+b5Z//wOqogHhayKfiQAAAABJRU5ErkJggg==";
        public AttendeeFixedQuestionsPublicationCard(CourseAttendance attendanceInfo)
        {
            this.Info = attendanceInfo;
        }

        public CourseAttendance Info { get; set; }
        public string ProfileImg { get; set; } = string.Empty;

        public override string GetCardContent()
        {
            var json = ReadResource(CardConstants.CardFileNameAttendeeFixedQuestionsPublication);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_ATTENDEE_NAME, this.Info.User.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_ATTENDEE_EMAIL, this.Info.User.Email);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QARole, this.Info.QARole);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QAOrg, this.Info.QAOrg);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QACountry, this.Info.QACountry);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QASpareTimeActivities, this.Info.QASpareTimeActivities);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QAMobilePhoneNumber, this.Info.QAMobilePhoneNumber);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_PROFILE_IMG, this.ProfileImg);

            
            return json;
        }

        public async Task LoadProfileImage(GraphServiceClient graphClient, string userId)
        {

            var loader = new UserDataLoader(graphClient);

            this.ProfileImg = await loader.GetUserPhotoBase64(userId) ?? DEFAULT_IMG;
        }

        public ChatMessageAttachment GetChatMessageAttachment()
        {
            return new ChatMessageAttachment
            {
                ContentType = AdaptiveCards.AdaptiveCard.ContentType,
                Content = this.GetCardContent(),
            };
        }
    }
}
