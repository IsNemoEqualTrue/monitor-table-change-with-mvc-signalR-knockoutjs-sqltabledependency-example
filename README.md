# Audit SQL Server table change by detect insert update delete and receive notifications

![alt text][shema]

[shema]: https://github.com/christiandelbianco/monitor-table-change-with-mvc-signalR-knockoutjs-sqltabledependency/blob/master/Splash-min.png "Notifications"

## Detect record table changes
One of the classic problems when writing software dealing with database is refreshing data.

Imagine a tool for display real time booking flight. After a first data access to retrieve records from database table, in order to maintain the cache up to date, further selects are needed over and over again. That is inefficient if the data rarely changes and it wastes resources and execution time on the database server.

Caching is one technique for minimizing demands on the database server. The data are queried once and stored in memory and the application then repeatedly accesses it. Occasionally, the cache is updated to refresh the data. The point is deciding when to update it. If we don’t do it often enough, users see old data; if we update too often, then we don’t optimally reduce our demand on the database.

**Wouldn't it be better if was our database to instantly notify our application about record changes, avoiding us to execute a periodical SELECT to refresh our server cache?**

Database table change notifications can help to solve this tricky problem. But how to monitor SQL Server table changes? Some options are:
* SQL Server Service Broker
* .NET SqlNotificationRequest
* .NET SqlDependency

All of them works based on a notifications infrastructure. The first two options require us a good T-SQL and database knowledge to create database objects as service broker and queue to monitor every change done on our records table and notify us about any record table change. Although the last one does not require us this low level knowledge, it presents a limitation: delivered notification does not report us any information about which record has been changed, forcing us to perform a further SELECT to refresh our cache.

**Is it possible receive record table change notifications containing modified, deleted or inserted records in order to avoid another SELECT to maintains update our cache?**

## Notification on table changes with SqlTableDependency
If we want get an alert about any table changes without paying attention to the underlying SQL Server infrastructure then SqlTableDependency's record table change notifications will do that for us. Using notifications, an application can detect record changes saving us from having to continuously re-query the database to get new values.

SqlTableDependency record change audit, provides the low-level implementation to receive database notifications creating SQL Server trigger, queue and service broker that immediately notify us when any record table changes happens.

For any record change, SqlTableDependency's event handler will get a notification containing modified table record values as well as the insert, update, delete operation type executed on our table.

Audit change with ASP.NET MVC, SignalR and Knockout JS
The following video show how to build a web application able to send real time notifications to clients. The code is visible below:

