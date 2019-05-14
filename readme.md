# Error PipelinesServer deadlock in FlushAsync 

# Steps to reproduce

* start PipelinesServer
* start WpfClient
  * Connect
  * "10_000 all newline"
    * sends 10_000 chars plus newline in one block
      * this works
  * "10_000 1000 newline"
    * sends 10_000 chars plus newline in blocks with size of 1000 chars
      * this works
  * "100_000 all newline"
    * sends 100_000 chars plus newline in one block
      * this works   
  * "100_000 100 newline"
    * sends 100_000 chars plus newline in blocks with size of 1000 chars
      * this does not works
        * DeadLock


