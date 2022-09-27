import axios from "axios"

export class ApiHelper{
    static getJson =(url) =>{
        return axios.get(url).then(response=>response);
    }  
}
