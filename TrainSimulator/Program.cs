using TrainSimulator.Domain;

Console.WriteLine("Hello, engineer!");
Console.WriteLine("Welcome to the Train Simulator.");
Console.WriteLine("Starting simulation...");

List<Station> mainRouteStations = new List<Station>()
{
    new Station("A", 0, false),
    new Station("B", 1, false),
    new Station("C", 2, false),
    new Station("D", 3, true),
};

Route mainRoute = new Route(1, "Ida", mainRouteStations);

List<Station> reverseRouteStations = new List<Station>()
{
    new Station("A", 3, true),
    new Station("B", 2, false),
    new Station("C", 1, false),
    new Station("D", 0, false),
};

Route reverseRoute = new Route(2, "Volta", reverseRouteStations);

List<Train> trains = new List<Train>() { new Train(1, "T1", mainRoute), new Train(1, "T2", reverseRoute) };



while (true)
{
    foreach(Train train in trains)
    {
        if(train.CurrentStation == null)
            train.StartRoute();

        train.ReturnLocation();
        if (!train.AdvanceToNextStation())
        {
            train.CurrentRoute = train.CurrentRoute.Id == 1 ? reverseRoute : mainRoute;
            train.StartRoute();
            Console.WriteLine("Arrived at final station, revesing now.");
        }
    }
   
    await Task.Delay(TimeSpan.FromSeconds(2));
}
    


