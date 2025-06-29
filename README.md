# Backlogs 0.9.2.0-alpha - dev branch

![](/Images/logo.png)

This is the fork of https://github.com/surya-sk/backlogs (Backlogs 0.9.2, dev branch). In 2025, I want to discover background tasks & notifications in this little and fun UWP app. I need it for my Melegram project where all notifications damaged, heh!

## Description
Keep track of films and TV series you want to watch, book you want to read, 
albums you want to listen to and games you want to play, all in one place. 
Backlogs is a simple Universal Windows Platform app that lets you manage all your backlogs in one place. 
Your backlogs are synced with all devices signed-in with your Microsoft account (if you sign-in). 
You will also get notifications to get to them on the target date.

## Screenshots
![](/Images/sshot01.png)
![](/Images/sshot02.png)
![](/Images/sshot03.png)

## Status
- After 3 years, I'm still trying to research toast notifications, especially for Lumia phone / Win10Mobile. I need to RnD last (newest) version of Backlogs project 
- I noticed than 1 notification appeared ! :)

## TODO
Notifications don't work in this (ms toasts' bugs?), I need to find another solution...

Fix all api keys (I use only fake API keys like 111 at now, except "IMdB" key)

Form Autofill (if "+" pressed at Books section then Books type needed to autoselect... IMHO))

## Tech req.
Recomended os build: 19xxx or above (22xxx if Win11?)
Minimal os build: 15063
Platforms: x86 (PC, etc.), x64 (PC, etc.), ARM (Win10M), ARM64 (WinRT)

## About the Original
Backlogs is a native Windows application that lets you manage your film, TV, music, 
game and book backlogs all in one place. 
The app supports cross-device syncing across devices signed in with your Microsoft account.

The application is available to download for Windows 10, Windows 10 Mobile, Windows 11, Xbox One and Xbox Series X.

<a href='https://www.microsoft.com/store/productId/9N2H8CM2KWVZ'><img src='https://developer.microsoft.com/en-us/store/badges/images/English_get-it-from-MS.png' alt='Store Link' height="50px"/></a>


### Description
This app is built using the Universal Windows Platform framework using C# and XAML. The controls use WinUI 2. The app implements the MVVM design pattern. The backlogs created are stored in JSON format in a txt file locally and on the user's OneDrive. A Singleton is used for managing the collection of backlogs and reading/writing to the save file. OneDrive storage is implemented using Microsoft Graph.

### Screenshots
<table><tr>
<td> <img src="Images/Mobile.png" alt="Drawing" style="width: 250px;"/> </td>
<td> <img src="Images/Desktop.png" alt="Drawing" style="width: 550px;"/> </td>
<td> <img src="Images/Desktop-light.png" alt="Drawing" style="width: 550px;"/> </td>
<td> <img src="Images/Desktop-all.png" alt="Drawing" style="width: 550px;"/> </td>
</tr></table>

### Building
The app requires Visual Studio 2017 and above to be built. 
You will need the Universal Windows Platform compnent installed. With all of that installed, just clone the repo and run the .sln file. 
The project will fail to compile because of a missing Keys.cs file. This file contains all the API keys, and is not checked in. Contact me if you would like access to the file, or create your own file if you have your own API keys. The file should be named Keys.cs and placed in the Utils folder and namespace. 

### Contributing and more
PRs are welcome! Make sure you create a new branch when making changes and pushing them. 
Please report any bugs in the Issues section. Include your OS, app version, 
a detailed description and steps to reproduce in the bug report.

### License 
This project is licensed under the GNU General Public License 3. Check LICENSE for more information.

## Referencies

https://github.com/surya-sk/backlogs Backlogs project

https://github.com/surya-sk Surya (surya-sk nickname), UWP and Game Developer 


Best wishes,

[M][E] June, 29 2025

![](/Images/footer.png)

