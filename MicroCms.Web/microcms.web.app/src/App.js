import React from 'react';
import { ApiHelper } from './components/apihelper';
import { NavigationLink } from './components/sidebar';
import GlobalConfig from './config';
import { Header } from './components/header';
import { BodyContainer } from './components/itemdetails';    

class MicroCmsApp extends React.Component{
    constructor(props){
        super(props);
        this.state = {            
            content:{fields:[]},
            sidebar: {
            links: [{
                link:"/somelink"}],
            row:0,
            col:0
            }}
    }
    componentDidMount(){
        ApiHelper.getJson(GlobalConfig.BaseUrl+ GlobalConfig.ChildItemsApiPath).then(res=>{
            this.setState({
                sidebar: {links:res.data}});
        })
    } 
    handleNavItemClick(id){
        ApiHelper.getJson(GlobalConfig.BaseUrl+GlobalConfig.ItemDetailsApiPath+id).then(res=> {
            if(res.data)
            {
                this.setState({content:res.data})
            }
        });
    }
    render(){
        const links= this.state.sidebar.links.map((l,i)=>{ return (<NavigationLink key={i} onClick={(id)=>this.handleNavItemClick(id)} link={l}/>); });
        return (                
                <div>  
                  <Header companyName="MicroCMS"/>  
                  <BodyContainer links={links} content={this.state.content}/>  
                </div>
        );
    }
};

export default MicroCmsApp;