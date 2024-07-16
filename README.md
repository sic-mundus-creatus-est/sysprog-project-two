<a id="readme-top"></a>

[![MIT License][license-shield]][license-url]




<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/sic-mundus-creatus-est/sysprog-project-two">
    <img src="images/task.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">sysprog-project-two</h3>

  <p align="center">
    The project's main goal was to familiarize with task usage and parallel execution in .NET Framework as part of the System Programming course.
    <br />
    <br />
    <br />
    <strong>Check out my solutions of other System Programming course projects »</strong>
    <br />
    <a href="https://github.com/sic-mundus-creatus-est/sysprog-project-one">sysprog-project-one</a>
    ·
    <a href="https://github.com/sic-mundus-creatus-est/sysprog-project-three">sysprog-project-three</a>
  </p>
</div>




<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About the Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation-and-setup">Installation and Setup</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#license">License</a></li>
  </ol>
</details>




<!-- ABOUT THE PROJECT -->
## About the Project

This project is an upgraded version of the previous one, utilizing tasks and async programming instead of raw threads. While the core assignment remains the same: counting words in a found file that start with an uppercase letter and have more than five characters — the implementation has been revamped. The overall structure is largely preserved, with adjustments made to integrate tasks and async functionality. This solution features an `HTTP web server` where each request is managed by a separate task. `Tasks` offer superior throughput by efficiently handling concurrent operations without immediate termination upon completion. They enable better resource utilization, improved responsiveness, and more efficient handling of concurrent operations compared to raw threads. The caching mechanism remains a simple `LRU` implementation but has been adapted for task and async operations, perhaps even excessively leveraging their capabilities. This transition to tasks and async programming provides a more scalable and efficient solution, harnessing modern concurrency paradigms.

<div style="text-align: center;">
  <img src="images/homepage.png" alt="Homepage" style="border-radius: 10px;">
</div>

Although a frontend component was not required for the assignment, I added it for ease of testing. The homepage is an list that displays all text files from the root directory and its subdirectories. This feature was created solely to facilitate testing and is not intended to serve as a practical web page example. Viewing the homepage with thousands of files is <strong>not recommended</strong> due to its easily extensible list.

</br>

- You can request the word count of a file by either clicking on a file name from the homepage or by simply entering the URL as: `http://localhost:18859/example.txt`

<div align="center">
    <img src="images/response_url.png" alt="Response URL" style="border-radius: 10px; display: block;">
    <p align="center" style="margin: 0;">After requesting the word count for an existing file, the response will display the word count stylized as a note pictured below:</p>
    <img src="images/response.png" alt="Response example" style="border-radius: 10px; display: block; margin-top: 10px;">
</div>

</br>

<div align="center" style="color: #ff69b4;">
    <p><em>All incorrect responses will return an appropriate HTTP error code.</em></p>
</div>

<p align="right"><a href="#readme-top">⬆️</a></p>

### Built With

* ![C#][CSharp] ![.NET][Dotnet]
* ![CSS][Css]
* ![HTML][Html]

<p align="right"><a href="#readme-top">⬆️</a></p>




<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these simple steps.

### Prerequisites

For the easiest setup, I recommend having Visual Studio installed and set up, with .NET 8.0 installed.

### Installation and Setup

1. Clone the repo
   ```sh
   git clone https://github.com/sic-mundus-creatus-est/sysprog-project-two.git
   ```
2. Open up the project solution file called `CapitalWordCounter.sln`
3. Navigate to Program.cs file
    - Option 1:
        - Create a folder called root in the following location: `sysprog-project-two\CapitalWordCounter\CapitalWordCounter\bin\Debug\net8.0` (or Release, depending on how you plan to run it)
    - Option 2:
        - Change the folder path from root to wherever you're going to be storing your test files
          ```csharp
          string rootFolder = @".\root"; // <== file searching always starts here, change it if needed
          ```
4. Place some .txt files in the chosen root folder (if you don't have any, I recommend Project Gutenberg, a digital library of books in many formats)
5. Check if the hard coded port `18859` is avaliable on your PC before running
   ```sh
   # To check if a port is avaliable on your PC:
             Windows: netstat -ano | findstr :18859
             Linux: netstat -tuln | grep 18859
   ```
   - If the port is already taken, change it here in the `Program.cs` file:
     ```csharp
     string serverURL = "http://localhost:[port]/"; // <== also a "homepage"
     ```
6. Finally, you're ready to start the server by pressing `Ctrl+F5` on your keyboard, or by clicking on the start button in the VS itself
    - _starting the server should open up the `homepage` on your default browser automatically_
7. To stop the server, simply press _any_ key

<br/>
<br/>

<div align="center" style="color: gray;">
  <i>you can also adjust the cache capacity in the CWC.Server.cs file within the constructor of CWC_Server</i>
</div>

<p align="right"><a href="#readme-top">⬆️</a></p>




<!-- USAGE EXAMPLES -->
## Usage

I still can't think of any useful or practical applications, but it can be a fun way to analyze your favorite books and pull out interesting trivia!

Comparing it to the previous implementation, this one is way more robust. By using tasks and async programming, this solution can handle significantly more simultaneous requests. Stress/load testing showed it can manage about anywhere near 2000 to about 4000 requests on a fresh start, and over 10,000 with cached items (on my PC configuration).

Though not fully optimized, this implementation serves as a reference for my coding progress. It also provided valuable practice in writing a README file, an essential skill for software documentation...

<p align="right"><a href="#readme-top">⬆️</a></p>




<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.

<p align="right"><a href="#readme-top">⬆️</a></p>




<!-- MARKDOWN LINKS & IMAGES -->
[license-shield]: https://img.shields.io/github/license/sic-mundus-creatus-est/sysprog-project-two?style=for-the-badge
[license-url]: https://github.com/sic-mundus-creatus-est/sysprog-project-two/blob/main/LICENSE

[CSharp]: https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white
[Dotnet]: https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fffS
[Css]: https://img.shields.io/badge/CSS-1572B6?logo=css3&logoColor=fff
[Html]: https://img.shields.io/badge/HTML-%23E34F26.svg?logo=html5&logoColor=white
