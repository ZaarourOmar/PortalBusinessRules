# Portal Business Rules - An Extension for Dynamics 365 PowerApps Portals
This is version 1 of my proposed solution to reducing the code needed for customizing Dynamics Portals. Portal Business Rules is a configuration-based solution to customize entity forms and web form steps by non technical user.

Installation Steps:
If you want to experiment with this solution as it is right now,
1) Install the managed solution "Portal Business Rules" in an instance that has the Portal Add-on already installed.
2) Create a web file with partial url ="portal-business-rules.js" and make the home page its parent page. In the note section, upload the following file.

If you want to change the solution, follow the above steps but install the unmanaged solution instead.


Current Limitations:
1) Each rule consists of a simple If/Else structure. If you want other conditions, you need to create more than one rule.
2) No visual designer for the rules and actions (like the one for CRM Business Rules). Everything is done using record based configuration. 

Near Future improvements road map:
1) Support the calling of another rule within a rule as an action.
2) Populate possible lookup values when the action is to set a field value for a lookup field. Currently we only support populating the possible values for options sets. 

