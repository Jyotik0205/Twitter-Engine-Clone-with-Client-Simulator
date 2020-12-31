# Twitter-Engine-Clone-with-Client-Simulator
Twitter Engine was built using Actors(Akka) in multiple processes to simulte client behviour and analyze performance of this distributed system. 


In this project, I implemented a Twitter Clone and a client tester/simulator.

I built an engine that should be paired up with WebSockets to provide full functionality like: 

1)Register account
2)Send tweet. Tweets can have hashtags (e.g. #COP5615isgreat) and mentions (@bestuser)
3)Subscribe to user's tweets
4)Re-tweets (so that your subscribers get an interesting tweet you got by other means)
5)Allow querying tweets subscribed to, tweets with specific hashtags, tweets in which the user is mentioned (my mentions)
6)If the user is connected, deliver the above types of tweets live (without querying)

A tester/simulator was Implemented to test the above

1) Simulated periods of live connection and disconnection for users
2) Simulated a Zipf distribution on the number of subscribers. For accounts with a lot of subscribers, increase the number of tweets. Made some of these messages re-tweets

The client part (send/receive tweets) and the engine (distribute tweets) have to be in separate processes. Preferably, you use multiple independent client processes that simulate thousands of clients and a single engine process


Results and Analysis in Report.Pdf
