X_PLATFORM
==========

CTA Execution Platform for use with X_TRADER Pro

![alt tag](https://raw.github.com/AntoniosHadji/X_PLATFORM/master/StrategyMonitorScreenShot.JPG)

This project started with the evaluation of the current operations of a specific CTA and was custom designed to automate as much of the work load as possible.  

Before using this application in production the workflow was:
  - Run a binary application which generated the system signals.  The source code to this binary was not available and the only output was a text report designed to be read by humans.
  - Manually submit appropriate orders in 18 markets.
  - Upon execution:
    - Submit next order manually
    - Manually collect fill information and allocate to 20+ customer accounts.
    - Email information to the clearing house for execution of allocation instructions.

The new process including this application:
  - Run the binary application to create the text report containing the signals.
  - An Excel sheet was created as a tool to import the text report and output XML data files for each of the 18 markets.  The Excell sheet was the original location of the customer account and allocation details which are now included in the XML data.
  - Start a batch script which launches one of these applications for each market traded.  The applications are placed in specific locations on the screen to appear as one seamless control panel for the CTA strategy.
      - Application automatically submits stop orders as specified by the system.  In certain markets the application monitored the markets and submitted the orders at specific triggers or as synthetic stop market orders.
      - The application automatically receives all fill information, allocates fills according to a specific algorithm, and emails the clearing firms automated parser service that is designed to accept specifically formatted messages via email.
      - The application prepares the next order and the process starts over.

This application was created in 2008 and remains in production today with only a few modifications for API updates.
