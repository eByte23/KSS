var Horseman = require('node-horseman');
var horseman = new Horseman();

horseman.cookies([
    {
    name: 'ASP.NET_SessionId',
    value: '1ayzwv1vl1iup5dpmuwneeju',
    domain: 'kissanime.ru'
  },
  {
    name: '__cfduid',
    value: 'dfc4f5806633abea0ee92c2e9be2c0ce01489567670',
    domain: '.kissanime.ru'
  },
  {
    name: 'cf_clearance',
    value: '3df2f0594409ca0a8d3487539e2f8be287a171f5-1489750539-86400',
    domain: '.kissanime.ru'
  },
  {
    name: 'usernameK',
    value: 'xG4TKgmC5MN8wGRtBG603wgekg3Q%2fre',
    domain: 'kissanime.ru'
  },
  {
    name: 'passwordK',
    value: '9ML9DyxT9jhwfHxqdSJICuKjfvs7%2f8St',
    domain: 'kissanime.ru'
  },
  ])
  .open('http://kissanime.ru/Anime/Dragon-Ball-Z/Episode-059?id=107007')
   .then(function(body) {
    console.log(body);
    /*
        {
        "authenticated": true,
        "user": "myUserName"
      }
    */
    return horseman.close();
  });

