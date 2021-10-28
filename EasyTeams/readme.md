# EasyTeams Bot

EasyTeams automates tasks in Teams via a bot that does things for you like setting-up meetings. Written in ASP.NET Core 3.1

## Usage
To create a conference call, just ask it:
> New conference call tomorrow at 10am

It'll ask for you details of the meeting, including who to add - internal & external.

## Installation

Download and open solution with Visual Studio 2019: EasyTeams.sln.
Solution is configured to use the dev environment already (keys to be secured correctly later). 

Contents include:
* EasyTeams.Bot - the bot web-project.
* EasyTeams.Common - common library between all projects.
* EasyTeams.Functions - Azure Functions. Just used to receive "create meeting" messages for adding events to calendars for meeting attendees.
* EasyTeams.Tests - Unit-tests and a test console.
* EasyTeams.Web - Simple website with a JavaScript client for the webchat.

## Requirements
Requires an Azure AD application registration with the following rights to Graph API:

Application permissions:
* Calendars.ReadWrite

Delegated permissions:
* User.Read
* OnlineMeetings.ReadWrite

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)
