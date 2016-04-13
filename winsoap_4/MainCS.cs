using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using winsoap_dal;
using System.IO;
using System.Reflection;

namespace winsoap_4
{
    class MainCS
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
            MainCS maincs = new MainCS();
            maincs.start_program();
        }

        DateTime dt_const = new DateTime(2014, 1, 1, 0, 0, 0);
        
        private void start_program()
        {
            
            ServiceSoap_1.HotelPricesRequest hotelpricereq1 = new ServiceSoap_1.HotelPricesRequest();
                ServiceSoap_1.User requestor1 = new ServiceSoap_1.User();
                    
                requestor1.Login = Properties.Settings.Default.login;
                requestor1.Password = Properties.Settings.Default.pass;
                requestor1.Name = "test1";
                hotelpricereq1.Requestor = requestor1;
            ServiceSoap_1.HotelPriceCondition hotelpricecond1 = new ServiceSoap_1.HotelPriceCondition();
                
                    ServiceSoap_1.ReferenceCondition refcity1 = new ServiceSoap_1.ReferenceCondition();
                  
                    ServiceSoap_1.ReferenceCondition refhotel1 = new ServiceSoap_1.ReferenceCondition();
                        ServiceSoap_1.DurationPeriod createddur1 = new ServiceSoap_1.DurationPeriod();
                        createddur1.BeginSpecified = true;
                        createddur1.EndSpecified = true;
                        createddur1.Begin = DateTime.Now.AddDays(-1); 
                        createddur1.End = DateTime.Now;
                hotelpricecond1.Created = createddur1;
                   
            hotelpricecond1.City = refcity1;
            
            hotelpricereq1.Conditions = new ServiceSoap_1.HotelPriceCondition[1];
                hotelpricereq1.Conditions[0] = hotelpricecond1;

            //hotelpriceresponse
            ServiceSoap_1.HotelPricesResponse hotelpriceresp1;
            //client
            ServiceSoap_1.ExchangeServiceSoapClient client1 = new ServiceSoap_1.ExchangeServiceSoapClient();
            
            

            //Делаем запрос hotelpricesrq()
            Console.WriteLine("Request...");
            hotelpriceresp1 = client1.HotelPricesRQ(hotelpricereq1);
            ServiceSoap_1.HotelPricesResponse hotelprs = hotelpriceresp1;

            Console.WriteLine("Response.");
            Console.WriteLine("Count_prices: {0}", hotelprs.Prices.Length);
            //Console.ReadLine();
            //return;

            if (hotelprs.Errors != null)
            {
                using (StreamWriter strwrite = File.AppendText("log.txt"))
                {
                    foreach (ServiceSoap_1.Warning er in hotelprs.Errors)
                    {
                        strwrite.Write(DateTime.Now.ToLongTimeString() + "    Error code: " + er.Code + ";    Error value:    " + er.Value + ";   ");
                        if (er.Code == 0)
                        {
                            strwrite.WriteLine("Проверьте правильность логина и пароля");
                        }
                    }
                }
                return;
            }

            using (StreamWriter strwrite = File.AppendText(AppDomain.CurrentDomain.BaseDirectory + @"\log.txt"))//Assembly.GetExecutingAssembly().Location
            {
                strwrite.WriteLine("\r\n_______________");
                strwrite.WriteLine(DateTime.Now.ToString() + " data received...\r\n processing...");
            }

            filling_BD2(hotelprs);


            using (StreamWriter strwrite = File.AppendText(AppDomain.CurrentDomain.BaseDirectory + @"log.txt"))
            {
                strwrite.WriteLine("  end: " + DateTime.Now.ToLongTimeString());
            }

        }

        private string to_string_sql(DateTime value)
        {
            return (value < dt_const) ? "NULL" : "'" + value + "'";
        }

        private void filling_BD2(ServiceSoap_1.HotelPricesResponse hotelprices)
        {
            Dictionary<string, int> dict_tables = new Dictionary<string, int>();
            dict_tables.Add("bookingperiod", 0);
            dict_tables.Add("city", 0);
            dict_tables.Add("hotel", 0);
            dict_tables.Add("mealtype", 0);
            dict_tables.Add("period", 0);
            dict_tables.Add("room", 0);
            dict_tables.Add("specialoffer", 0);
            dict_tables.Add("price", 0);

            winsoap_dal.winsoap_conn_dal win_conn_dal = new winsoap_conn_dal();
            string str_cn = Properties.Settings.Default.SQL_conn_str;//ConfigurationManager.ConnectionStrings["sql_provider1"].ConnectionString;


            var hotelprices_sorted = hotelprices.Prices.OrderBy(item => item.InternalID);

            win_conn_dal.OpenConnection(str_cn);
            int company_last_id = win_conn_dal.select_company_last_id(Properties.Settings.Default.id_company_in_BD);

            int index_of_current_element = 0;
            foreach (ServiceSoap_1.HotelPrice srt in hotelprices_sorted)
            {
                Console.Write(index_of_current_element++ + "\r");

                if (srt.InternalID <= company_last_id)
                {
                    continue;
                }
                else 
                {
                    company_last_id = srt.InternalID;
                    win_conn_dal.update_company_last_id(company_last_id, Properties.Settings.Default.id_company_in_BD);
                }

                if (srt.City.Name == null &&
                            srt.Hotel.Name == null &&
                            srt.Amount == 0 &&
                            srt.Currency == null)
                {
                    continue;
                }
                //SELECT Проверка наличия. Не проверяем только bookingperiod, prices, company, links
                dict_tables["bookingperiod"] = win_conn_dal.select_bookingperiod(srt.BookingPeriod.Begin, 
                                                                                   srt.BookingPeriod.End);
                
                dict_tables["city"] = win_conn_dal.select_city(srt.City.Code, srt.City.Name);
                dict_tables["hotel"] = win_conn_dal.select_hotel(srt.Hotel.Code,
                                                                    srt.Hotel.Name,
                                                                    srt.Hotel.Category.Name);
                dict_tables["mealtype"] = win_conn_dal.select_mealtype(srt.MealType.Code,
                                                                        srt.MealType.Name);
                dict_tables["period"] = win_conn_dal.select_period(srt.Period.Begin,
                                                                    srt.Period.End);
                dict_tables["room"] = win_conn_dal.select_room(srt.Room.Accommodation.Code,
                                                                srt.Room.Accommodation.Name,
                                                                srt.Room.Category.Code,
                                                                srt.Room.Category.Name,
                                                                srt.Room.Type.Code,
                                                                srt.Room.Type.Name);
                dict_tables["specialoffer"] = win_conn_dal.select_specialoffer(srt.SpecialOffer.Name,
                                                                                srt.SpecialOffer.Code,
                                                                                srt.SpecialOffer.FreeNights,
                                                                                srt.SpecialOffer.SpecialOfferType.Code,
                                                                                srt.SpecialOffer.SpecialOfferType.Name);


                dict_tables["price"] = win_conn_dal.insert_price(srt.Created,
                                                                 srt.Amount,
                                                                 srt.Currency,
                                                                 srt.MinimumStay,
                                                                 srt.MaximumStay,
                                                                 srt.Type.ToString());
                //INSERTS прошедшие SELECT
                if (dict_tables["city"] == 0)
                {
                    dict_tables["city"] = win_conn_dal.insert_city(srt.City.Code, srt.City.Name);
                }

                if (dict_tables["hotel"] == 0)
                {
                    dict_tables["hotel"] = win_conn_dal.insert_hotel(srt.Hotel.Code,
                                                                    srt.Hotel.Name,
                                                                    srt.Hotel.Category.Name);
                }

                if (dict_tables["mealtype"] == 0)
                {
                    dict_tables["mealtype"] = win_conn_dal.insert_mealtype(srt.MealType.Code,
                                                                        srt.MealType.Name);
                }

                if (dict_tables["bookingperiod"] == 0)
                {
                    if (srt.BookingPeriod != null)
                    {
                        dict_tables["bookingperiod"] = win_conn_dal.insert_bookingperiod(srt.BookingPeriod.Begin,
                                                                        srt.BookingPeriod.End);
                    }
                }

                if (dict_tables["period"] == 0)
                {
                    if (srt.Period != null)
                    {
                        dict_tables["period"] = win_conn_dal.insert_period(srt.Period.Begin,
                                                                        srt.Period.End);
                    }
                }

                if (dict_tables["room"] == 0)
                {
                    dict_tables["room"] = win_conn_dal.insert_room(srt.Room.Accommodation.Code,
                                                                srt.Room.Accommodation.Name,
                                                                srt.Room.Category.Code,
                                                                srt.Room.Category.Name,
                                                                srt.Room.Type.Code,
                                                                srt.Room.Type.Name);
                }

                if (dict_tables["specialoffer"] == 0)
                {
                    dict_tables["specialoffer"] = win_conn_dal.insert_specialoffer(srt.SpecialOffer.Name,
                                                                                srt.SpecialOffer.Code,
                                                                                srt.SpecialOffer.FreeNights,
                                                                                srt.SpecialOffer.SpecialOfferType.Code,
                                                                                srt.SpecialOffer.SpecialOfferType.Name);
                }

                //INSERTS links
                win_conn_dal.insert_link(dict_tables["hotel"],
                                          dict_tables["city"],
                                          dict_tables["room"],
                                          dict_tables["price"],
                                          dict_tables["mealtype"],
                                          dict_tables["bookingperiod"],
                                          dict_tables["period"],
                                          dict_tables["specialoffer"],
                                          Properties.Settings.Default.id_company_in_BD,//dict_tables["company"]);
                                          Properties.Settings.Default.id_country_in_BD);
            }
            win_conn_dal.CloseConnection();
        }
    }
}
