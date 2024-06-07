# <h1 align="center"> MetaExchange </h1>

<p style="text-align: center;"> <b>MetaExchange</b> is a piece of code that always gives the user the best possible price if he is buying or selling a certain amount of BTC. </p>

## 🚀 Quickstart

> git clone https://github.com/filipzupancic/meta-exchange

- **Run MetaExchangeApi**

```
// Assuming you are in the root of the project and running from terminal.

> cd MetaExchangeApi/

// with Docker assuming you have docker Desktop installed
> docker-compose up --build

// without Docker

Add .env file and just copy what's in .env.example
Unzip order_books_data.7z in Data/ folder

> dotnet restore
> dotnet build
> dotnet run

Application swagger UI should be accessible on http://localhost:5057/swagger/index.html

Both GET and POST endpoints take two parameters:
Amount which is BTC: ex. 10
Type which is a string: Sell or Buy

You can also test the endpoint through Postman or with curl:
curl -X 'GET' \
  'http://localhost:5057/api/MetaExchange/quote?amount=10&type=Sell' \
  -H 'accept: text/plain'

curl -X 'POST' \
  'http://localhost:5057/api/MetaExchange/trade?amount=10&type=Buy' \
  -H 'accept: text/plain' \
  -d ''
```

- **Run MetaExchangeConsole**

```
// We only Dockerised MetaExchangeApi because it makes the most
// sense. MetaExchangeConsole was used mainly for debugging.

Unzip order_books_data.7z in MetaExchangeApi/Data/ folder
> cd MetaExchangeConsole/
> dotnet restore
> dotnet build
> dotnet run

```

- **Run MetaExchangeTest**

```
// There are two tests and they should pass :D

> cd MetaExchangeTest/
> dotnet restore
> dotnet build
> dotnet run
```

## 📦 Structure

**MetaExchangeApi**: Web service (Kestrel, .NET Core API) which follows Model–view–controller (MVC) design pattern with added support for Docker so It can be run in a docker container.

- **Controllers/MetaExchangeController** class handles the logic for quoting the best prices for trades, utilizing MetaExchangeService service to load order books and match trades efficiently. It employs caching to store order books, reducing the need for repeated loading from the file system, and features a GET and POST endpoints that provide trade pricing information based on user-specified criteria.

- **Services/MetaExchangeService** class handles order book loading and order matching.

- **Models/OrderBook** is where classes that represent OrderBook object are defined.

- **Data/** contains order_books_data.7z. It is compressed due to GitHub size limits.

**MetaExchangeConsole**: This is a simple console project that holds similar logic to the MetaExchangeApi and is useful for debugging as it isolates core logic from web api features.

**MetaExchangeTest**: Simple unit tests for verification of the order matching logic.

## 🙋‍♂️ Disclaimer

I have read the assignment carefully and I believe all parts are implemented. There are many potential improvements but I wanted to strike the balance between
the delivery time and quality of code. If there were any doubts I added the comment in the code explaining why I wrote what I wrote. Code is documented a bit too much for my taste but I wanted to explain my thought process as precisely as possible. Normally I don't write so many comments. I am willing to defend my solution or discuss it in the office in case there are any questions or design features you would implement differently. I hope I did well, cheers!
